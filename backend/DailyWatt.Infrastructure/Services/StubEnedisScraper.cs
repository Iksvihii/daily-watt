using DailyWatt.Domain.Services;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System;
using System.IO;
using System.Linq;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Puppeteer-based scraper that navigates through the Enedis portal to download the consumption CSV.
/// Steps:
/// 1. Navigate to https://www.enedis.fr/particulier
/// 2. Click button with class "monCompte nav-link-button"
/// 3. Wait for redirect to login page
/// 4. Fill login (idToken1) and password (idToken2)
/// 5. Click submit button (idToken4)
/// 6. Verify redirect to home-connectee or handle login error
/// 7. Navigate to consumption page
/// 8. Find meter selector by PRM number
/// 9. Click on meter vignette
/// 10. Download consumption file
/// </summary>
public class StubEnedisScraper : IEnedisScraper
{
    private readonly ILogger<StubEnedisScraper> _logger;
    private const string InitialUrl = "https://www.enedis.fr/particulier";
    private const string HomeConnecteeUrl = "https://mon-compte-particulier.enedis.fr/home-connectee";
    private const string ConsumptionPageUrl = "https://mon-compte-particulier.enedis.fr/visualiser-vos-mesures-consommation";

    public StubEnedisScraper(ILogger<StubEnedisScraper> logger)
    {
        _logger = logger;
    }

    public async Task<Stream> DownloadConsumptionCsvAsync(string login, string password, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var executablePath = GetBrowserExecutablePath();
        if (string.IsNullOrEmpty(executablePath))
        {
            throw new InvalidOperationException("No Chromium/Edge/Chrome executable found. Set PUPPETEER_EXECUTABLE_PATH or install Edge/Chrome.");
        }

        _logger.LogInformation("Using browser executable at {ExecutablePath}", executablePath);

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = executablePath,
            Args = new[] { "--no-sandbox" }
        });

        var browserVersion = await browser.GetVersionAsync();
        _logger.LogInformation("Launched browser: {BrowserVersion}", browserVersion);

        await using var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = 1920,
            Height = 1080,
            DeviceScaleFactor = 1,
            IsMobile = false
        });
        var downloadDir = Path.Combine(Path.GetTempPath(), $"dailywatt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(downloadDir);

        // Configure download behavior
        var client = await page.Target.CreateCDPSessionAsync();
        await client.SendAsync("Page.setDownloadBehavior", new
        {
            behavior = "allow",
            downloadPath = downloadDir
        });

        try
        {
            // Step 1: Navigate to Enedis main page
            _logger.LogInformation("Step 1: Navigating to {Url}", InitialUrl);
            await NavigateWithReferrerPolicyAsync(page, client, InitialUrl, ct);
            await TakeScreenshotAsync(page, "01-enedis-main-page.png", downloadDir);

            // Step 1.5: Handle cookie consent if present (with short wait)
            _logger.LogInformation("Step 1.5: Checking for cookie consent popup");
            await AcceptCookiesIfPresentAsync(page, ct);
            await TakeScreenshotAsync(page, "01b-cookies-accepted.png", downloadDir);

            // Step 2: Click on "Mon Compte" button
            _logger.LogInformation("Step 2: Clicking on 'Mon Compte' link with class 'monCompte nav-link-button'");
            var monCompteLink = await page.QuerySelectorAsync("a.monCompte.nav-link-button");
            if (monCompteLink == null)
            {
                _logger.LogError("Step 2 Failed: 'Mon Compte' link not found");
                await TakeScreenshotAsync(page, "02-error-moncompte-link-not-found.png", downloadDir);
                throw new InvalidOperationException("'Mon Compte' link with class 'monCompte nav-link-button' not found on page");
            }

            // Click and verify redirect target by URL host
            await page.ClickAsync("a.monCompte.nav-link-button");
            var expectedBase = "https://mon-compte-client.enedis.fr/";
            var startWait = DateTime.UtcNow;
            while (DateTime.UtcNow - startWait < TimeSpan.FromSeconds(15))
            {
                var current = page.Url;
                if (current.StartsWith(expectedBase, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                await Task.Delay(250);
            }
            await TakeScreenshotAsync(page, "03-after-moncompte-click.png", downloadDir);

            // Step 3: Accept cookies on auth page first, then wait for login form
            _logger.LogInformation("Step 3: Handling cookie consent on auth page, then waiting for login form");
            await AcceptCookiesIfPresentAsync(page, ct);
            await page.WaitForSelectorAsync("#idToken1", new() { Timeout = 10000 }).WaitAsync(ct);
            await TakeScreenshotAsync(page, "04-login-form-loaded.png", downloadDir);

            // Step 4: Fill login credentials with anti-robot flow
            _logger.LogInformation("Step 4: Filling login credentials");
            var loginInput = await page.QuerySelectorAsync("#idToken1");
            var passwordInput = await page.QuerySelectorAsync("#idToken2");
            if (loginInput == null)
            {
                _logger.LogError("Step 4 Failed: Login input not found");
                await TakeScreenshotAsync(page, "05-error-login-input-not-found.png", downloadDir);
                throw new InvalidOperationException("Login input (idToken1) not found on page");
            }

            // Case A: password already present → fill both and proceed
            if (passwordInput != null)
            {
                _logger.LogInformation("Password input present; filling both fields");
                await page.TypeAsync("#idToken1", login, new() { Delay = 50 });
                await page.TypeAsync("#idToken2", password, new() { Delay = 50 });
                await TakeScreenshotAsync(page, "06-credentials-filled.png", downloadDir);
            }
            else
            {
                // Case B: anti-robot pre-step → type login, wait, click verification, then wait for password
                _logger.LogInformation("Password input missing; performing anti-robot pre-step");
                await page.TypeAsync("#idToken1", login, new() { Delay = 50 });
                await TakeScreenshotAsync(page, "06-login-typed.png", downloadDir);

                // Small wait to let page render the anti-bot control
                await Task.Delay(1500, ct);

                // Step 1: Locate the captcha widget and clickable element
                _logger.LogInformation("Locating captcha widget and clickable element");
                var captchaWidget = await page.QuerySelectorAsync("#captcha-widget");
                if (captchaWidget != null)
                {
                    await TakeScreenshotAsync(page, "06a-captcha-widget-found.png", downloadDir);

                    // Step 2: Try to find the clickable element containing "Clique ici pour vérifier"
                    var clickableElement = await page.EvaluateFunctionAsync<string?>(
                        @"() => {
                            const widget = document.querySelector('#captcha-widget');
                            if (!widget) return null;
                            
                            // Search all clickable elements within the widget
                            const candidates = widget.querySelectorAll('button, a, div[role=""button""], div[onclick]');
                            for (const el of candidates) {
                                const text = (el.textContent || '').trim().toLowerCase();
                                if (text.includes('clique ici pour vérifier')) {
                                    // Return a unique selector or class
                                    return el.id || el.className || 'first-match';
                                }
                            }
                            return null;
                        }"
                    );

                    if (clickableElement != null)
                    {
                        _logger.LogInformation("Found clickable element in captcha widget: {Element}", clickableElement);

                        // Step 3: Click using Puppeteer (not JavaScript) to simulate real user interaction
                        var captchaTagged = await page.EvaluateFunctionAsync<bool>(
                            @"() => {
                                const widget = document.querySelector('#captcha-widget');
                                if (!widget) return false;
                                
                                const candidates = widget.querySelectorAll('button, a, div[role=""button""], div[onclick]');
                                for (const el of candidates) {
                                    const text = (el.textContent || '').trim().toLowerCase();
                                    if (text.includes('clique ici pour vérifier')) {
                                        el.setAttribute('data-captcha-trigger', 'true');
                                        return true;
                                    }
                                }
                                return false;
                            }"
                        );

                        if (captchaTagged)
                        {
                            // Use Puppeteer's click instead of JavaScript click
                            var triggerElement = await page.QuerySelectorAsync("[data-captcha-trigger='true']");
                            if (triggerElement != null)
                            {
                                await triggerElement.ClickAsync();
                                _logger.LogInformation("Clicked captcha trigger with Puppeteer");
                                await Task.Delay(1000, ct);
                                await TakeScreenshotAsync(page, "06b-after-captcha-click.png", downloadDir);

                                // Step 4: Check captcha status (success, failure, or pending)
                                var captchaStatus = await page.EvaluateFunctionAsync<string>(
                                    @"() => {
                                        const widget = document.querySelector('#captcha-widget');
                                        if (!widget) return 'not-found';
                                        const text = widget.textContent?.toLowerCase() || '';
                                        
                                        // Check for failure messages first
                                        if (text.includes('échec') || text.includes('failed') || text.includes('try a different browser')) {
                                            return 'failed';
                                        }
                                        
                                        // Check for verification UI
                                        if (text.includes('je ne suis pas un robot') || text.includes('vérification')) {
                                            return 'pending';
                                        }
                                        
                                        return 'unknown';
                                    }"
                                );

                                _logger.LogInformation("Captcha status after click: {Status}", captchaStatus);

                                if (captchaStatus == "failed")
                                {
                                    _logger.LogError("CAPTCHA VERIFICATION FAILED - Anti-bot detected Puppeteer/automation");
                                    await TakeScreenshotAsync(page, "06b-captcha-failed-detected.png", downloadDir);
                                    throw new InvalidOperationException("Captcha verification failed: Anti-bot system detected automation. Solutions: 1) Set Headless=false for manual solving, 2) Use puppeteer-extra-plugin-stealth, 3) Contact Enedis for API access.");
                                }
                                else if (captchaStatus == "pending")
                                {
                                    _logger.LogInformation("Verification UI appeared - waiting for captcha resolution");
                                    // Wait for captcha to resolve (check for solution token or completion indicator)
                                    var resolved = false;
                                    var captchaStart = DateTime.UtcNow;
                                    while (DateTime.UtcNow - captchaStart < TimeSpan.FromSeconds(30))
                                    {
                                        // Check if password field became available (indicates captcha solved)
                                        var pwdCheck = await page.QuerySelectorAsync("#idToken2");
                                        if (pwdCheck != null)
                                        {
                                            resolved = true;
                                            break;
                                        }

                                        // Or check for captcha solution token/completion
                                        var captchaSolved = await page.EvaluateFunctionAsync<bool>(
                                            @"() => {
                                                const widget = document.querySelector('#captcha-widget');
                                                if (!widget) return false;
                                                // FriendlyCaptcha or similar adds a solution field
                                                const solution = widget.querySelector('input[name*=""captcha""], input[type=""hidden""]');
                                                return solution && solution.value && solution.value.length > 10;
                                            }"
                                        );

                                        if (captchaSolved)
                                        {
                                            resolved = true;
                                            break;
                                        }

                                        await Task.Delay(1000, ct);
                                    }

                                    if (resolved)
                                    {
                                        _logger.LogInformation("Captcha appears to be resolved");
                                        await TakeScreenshotAsync(page, "06c-captcha-resolved.png", downloadDir);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Captcha resolution timeout - proceeding anyway");
                                        await TakeScreenshotAsync(page, "06c-captcha-timeout.png", downloadDir);
                                    }
                                }

                                // Step 5: Try to submit the pre-step
                                var preSubmit = await page.QuerySelectorAsync("#idToken3_0");
                                if (preSubmit != null)
                                {
                                    await Task.Delay(800, ct);
                                    await preSubmit.ClickAsync();
                                    _logger.LogInformation("Clicked pre-step submit (#idToken3_0)");
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No clickable element with verification text found in captcha widget");
                    }
                }
                else
                {
                    _logger.LogWarning("Captcha widget (#captcha-widget) not found on page");
                }

                // Wait for password to become available
                var start = DateTime.UtcNow;
                while (DateTime.UtcNow - start < TimeSpan.FromSeconds(20))
                {
                    passwordInput = await page.QuerySelectorAsync("#idToken2");
                    if (passwordInput != null)
                    {
                        break;
                    }
                    await Task.Delay(500, ct);
                }
                await TakeScreenshotAsync(page, "06b-post-anti-robot.png", downloadDir);

                if (passwordInput == null)
                {
                    _logger.LogError("Step 4 Failed: Password input still not available after anti-robot");
                    await TakeScreenshotAsync(page, "06c-error-password-missing.png", downloadDir);
                    throw new InvalidOperationException("Password input (idToken2) not available after anti-robot step");
                }

                await page.TypeAsync("#idToken2", password, new() { Delay = 50 });
                await TakeScreenshotAsync(page, "06d-credentials-filled.png", downloadDir);
            }

            // Step 5: Click submit button
            _logger.LogInformation("Step 5: Clicking submit button (idToken4)");
            var submitButton = await page.QuerySelectorAsync("#idToken4");
            if (submitButton == null)
            {
                _logger.LogError("Step 5 Failed: Submit button not found");
                await TakeScreenshotAsync(page, "07-error-submit-button-not-found.png", downloadDir);
                throw new InvalidOperationException("Submit button (idToken4) not found on page");
            }

            await Task.WhenAll(
                page.ClickAsync("#idToken4"),
                page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, Timeout = 30000 })
            );

            // Step 6: Verify redirect to home-connectee or handle error
            _logger.LogInformation("Step 6: Verifying successful login redirect");
            await Task.Delay(2000); // Wait for page to fully load
            var currentUrl = page.Url;
            _logger.LogInformation("Current URL after login: {CurrentUrl}", currentUrl);

            if (!currentUrl.Contains("home-connectee"))
            {
                // Check for login error message
                var errorDiv = await page.QuerySelectorAsync("#lbErrorMessage");
                var errorMessage = "Unknown error";
                if (errorDiv != null)
                {
                    errorMessage = await page.EvaluateFunctionAsync<string>(
                        @"(selector) => document.querySelector(selector)?.textContent || 'Unknown error'",
                        "#lbErrorMessage"
                    );
                }

                _logger.LogError("Step 6 Failed: Login error - {ErrorMessage}", errorMessage);
                await TakeScreenshotAsync(page, "08-error-login-failed.png", downloadDir);
                throw new InvalidOperationException($"Login failed: {errorMessage}");
            }

            await TakeScreenshotAsync(page, "09-login-success.png", downloadDir);

            // Step 7: Navigate to consumption page
            _logger.LogInformation("Step 7: Navigating to consumption page {Url}", ConsumptionPageUrl);
            await NavigateWithReferrerPolicyAsync(page, client, ConsumptionPageUrl, ct);
            await Task.Delay(1500); // Wait for page to render
            await TakeScreenshotAsync(page, "10-consumption-page-loaded.png", downloadDir);

            // Step 8: Find meter selector by PRM number
            _logger.LogInformation("Step 8: Looking for meter selector with PRM {PRM}", "not specified");
            var selectionCompteur = await page.QuerySelectorAsync("selection-compteur");
            if (selectionCompteur == null)
            {
                _logger.LogError("Step 8 Failed: 'selection-compteur' element not found");
                await TakeScreenshotAsync(page, "11-error-selection-compteur-not-found.png", downloadDir);
                throw new InvalidOperationException("'selection-compteur' element not found on page");
            }

            // Find all <p> tags inside selection-compteur
            var pElements = await page.EvaluateFunctionAsync<dynamic[]>(
                @"() => {
                    const container = document.querySelector('selection-compteur');
                    if (!container) return [];
                    const pTags = container.querySelectorAll('p');
                    return Array.from(pTags).map(p => ({
                        text: p.textContent,
                        html: p.outerHTML
                    }));
                }"
            );

            if (pElements.Length == 0)
            {
                _logger.LogError("Step 8 Failed: No <p> elements found in 'selection-compteur'");
                await TakeScreenshotAsync(page, "12-error-no-p-elements.png", downloadDir);
                throw new InvalidOperationException("No meter numbers found in selection-compteur");
            }

            // Log found meters
            _logger.LogInformation("Found {Count} meter(s) in selection-compteur", pElements.Length);
            foreach (var elem in pElements)
            {
                string text = (elem?["text"] as object)?.ToString() ?? "unknown";
                _logger.LogInformation("Found meter text: {MeterText}", text);
            }

            // Step 9: Click on the meter vignette
            _logger.LogInformation("Step 9: Clicking on meter vignette");
            var prmElement = pElements.FirstOrDefault();
            if (prmElement == null)
            {
                _logger.LogError("Step 9 Failed: No PRM element found");
                await TakeScreenshotAsync(page, "13-error-no-prm-found.png", downloadDir);
                throw new InvalidOperationException("No PRM found in meters");
            }

            // Click on parent lnc-visa-vignette-group of the p element
            var clicked = await page.EvaluateFunctionAsync<bool>(
                @"() => {
                    const pTags = document.querySelectorAll('selection-compteur p');
                    for (let p of pTags) {
                        if (p.textContent.includes('n°')) {
                            let parent = p.closest('lnc-visa-vignette-group');
                            if (parent) {
                                parent.click();
                                return true;
                            }
                        }
                    }
                    return false;
                }"
            );

            if (!clicked)
            {
                _logger.LogError("Step 9 Failed: Could not click meter vignette");
                await TakeScreenshotAsync(page, "14-error-vignette-click-failed.png", downloadDir);
                throw new InvalidOperationException("Failed to click on meter vignette");
            }

            await Task.Delay(1500); // Wait for page to update
            await TakeScreenshotAsync(page, "15-meter-selected.png", downloadDir);

            // Step 10: Find and click download button
            _logger.LogInformation("Step 10: Looking for download button");
            var downloadClicked = await page.EvaluateFunctionAsync<bool>(
                @"() => {
                    const buttons = document.querySelectorAll('button');
                    for (let btn of buttons) {
                        const span = btn.querySelector('span');
                        if (span && span.textContent.includes('Télécharger')) {
                            btn.click();
                            return true;
                        }
                    }
                    return false;
                }"
            );

            if (!downloadClicked)
            {
                _logger.LogError("Step 10 Failed: Download button not found");
                await TakeScreenshotAsync(page, "16-error-download-button-not-found.png", downloadDir);
                throw new InvalidOperationException("Download button not found on page");
            }

            _logger.LogInformation("Step 10: Download button clicked");
            await TakeScreenshotAsync(page, "17-download-initiated.png", downloadDir);

            // Step 11: Wait for file download
            _logger.LogInformation("Step 11: Waiting for file download to complete");
            var downloadedFile = await WaitForDownloadAsync(downloadDir, TimeSpan.FromSeconds(30), ct);
            if (downloadedFile == null)
            {
                _logger.LogError("Step 11 Failed: Download did not complete within timeout");
                await TakeScreenshotAsync(page, "18-error-download-timeout.png", downloadDir);
                throw new InvalidOperationException("Download did not complete within timeout (30 seconds)");
            }

            _logger.LogInformation("Step 11: File downloaded successfully: {FileName}", Path.GetFileName(downloadedFile));
            await TakeScreenshotAsync(page, "19-download-complete.png", downloadDir);

            // Read file content and return as stream
            var bytes = await File.ReadAllBytesAsync(downloadedFile, ct);
            _logger.LogInformation("Successfully read {FileSize} bytes from downloaded file", bytes.Length);

            return new MemoryStream(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Enedis scraping: {ErrorMessage}", ex.Message);
            await TakeScreenshotAsync(page, "error-final-exception.png", downloadDir);
            throw;
        }
    }

    private static async Task TakeScreenshotAsync(IPage page, string fileName, string downloadDir)
    {
        try
        {
            var screenshotPath = Path.Combine(downloadDir, fileName);
            await page.ScreenshotAsync(screenshotPath);
        }
        catch (Exception ex)
        {
            // Log but don't fail the scraping if screenshot fails
            Console.WriteLine($"Failed to take screenshot {fileName}: {ex.Message}");
        }
    }

    private static async Task AcceptCookiesIfPresentAsync(IPage page, CancellationToken ct)
    {
        // Allow time for the cookie popup to render
        await Task.Delay(800, ct);
        try
        {
            var cookieButton = await page.QuerySelectorAsync("#popin_tc_privacy_button_3");
            if (cookieButton != null)
            {
                await page.ClickAsync("#popin_tc_privacy_button_3");
                await Task.Delay(800, ct);
            }
        }
        catch
        {
            // Non-fatal
        }
    }

    private static async Task NavigateWithReferrerPolicyAsync(IPage page, ICDPSession client, string url, CancellationToken ct)
    {
        var navigationTask = page.WaitForNavigationAsync(new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }
        });

        await client.SendAsync("Page.navigate", new
        {
            url
        });

        await navigationTask.WaitAsync(ct);
    }

    private static string? GetBrowserExecutablePath()
    {
        // Priority 1: explicit env var
        var envPath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        // Priority 2: Edge stable
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe")
        };

        var found = candidates.FirstOrDefault(File.Exists);
        return found;
    }

    private static async Task<string?> WaitForDownloadAsync(string folder, TimeSpan timeout, CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            ct.ThrowIfCancellationRequested();
            var files = Directory.GetFiles(folder);
            var downloadedFile = files.FirstOrDefault(f =>
                !f.EndsWith(".crdownload", StringComparison.OrdinalIgnoreCase) &&
                !f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            );

            if (downloadedFile != null)
            {
                return downloadedFile;
            }

            await Task.Delay(500, ct);
        }

        return null;
    }
}
