// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

[CollectionDefinition(nameof(InteractivityTest), DisableParallelization = true)]
public class NoInteractivityTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    public NoInteractivityTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void NavigationManagerCanRefreshSSRPageWhenInteractivityNotPresent()
    {
        Navigate($"{ServerPathBase}/forms/form-that-calls-navigation-manager-refresh");

        var guid = Browser.Exists(By.Id("guid")).Text;

        Browser.Exists(By.Id("submit-button")).Click();

        // Checking that the page was refreshed.
        // The redirect request method is GET.
        // Providing a Guid to check that it is not the initial GET request for the page
        Browser.NotEqual(guid, () => Browser.Exists(By.Id("guid")).Text);
        Browser.Equal("GET", () => Browser.Exists(By.Id("method")).Text);
    }

    [Fact]
    public void CanUseServerAuthenticationStateByDefault()
    {
        Navigate($"{ServerPathBase}/auth/static-authentication-state");

        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive")).Text);
        Browser.Equal("Static", () => Browser.FindElement(By.Id("platform")).Text);

        Browser.Equal("False", () => Browser.FindElement(By.Id("identity-authenticated")).Text);
        Browser.Equal("", () => Browser.FindElement(By.Id("identity-name")).Text);
        Browser.Equal("(none)", () => Browser.FindElement(By.Id("test-claim")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-in-test-role-1")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-in-test-role-2")).Text);

        Browser.Click(By.LinkText("Log in"));

        Browser.Equal("True", () => Browser.FindElement(By.Id("identity-authenticated")).Text);
        Browser.Equal("YourUsername", () => Browser.FindElement(By.Id("identity-name")).Text);
        Browser.Equal("Test claim value", () => Browser.FindElement(By.Id("test-claim")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-in-test-role-1")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-in-test-role-2")).Text);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void NavigatesWithoutInteractivityByRequestRedirection(bool controlFlowByException, bool isStreaming)
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.EnableThrowNavigationException", isEnabled: controlFlowByException);
        string streaming = isStreaming ? $"streaming-" : "";
        Navigate($"{ServerPathBase}/routing/ssr-{streaming}navigate-to");
        Browser.Equal("Click submit to navigate to home", () => Browser.Exists(By.Id("test-info")).Text);
        Browser.Click(By.Id("redirectButton"));
        Browser.Equal("Routing test cases", () => Browser.Exists(By.Id("test-info")).Text);
    }

    [Fact]
    public void CanRenderNotFoundPageAfterStreamingStarted()
    {
        Navigate($"{ServerPathBase}/streaming-set-not-found");
        Browser.Equal("Default Not Found Page", () => Browser.Title);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProgrammaticNavigationToNotExistingPathReExecutesTo404(bool streaming)
    {
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found-ssr{streamingPath}?navigate-programmatically=true");
        Assert404ReExecuted();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LinkNavigationToNotExistingPathReExecutesTo404(bool streaming)
    {
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/redirection-not-found-ssr{streamingPath}");
        Browser.Click(By.Id("link-to-not-existing-page"));
        Assert404ReExecuted();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BrowserNavigationToNotExistingPathReExecutesTo404(bool streaming)
    {
        // non-existing path has to have re-execution middleware set up
        // so it has to have "reexecution" prefix. Otherwise middleware mapping
        // will not be activated, see configuration in Startup
        string streamingPath = streaming ? "-streaming" : "";
        Navigate($"{ServerPathBase}/reexecution/not-existing-page-ssr{streamingPath}");
        Assert404ReExecuted();
    }

    private void Assert404ReExecuted() =>
        Browser.Equal("Welcome On Page Re-executed After Not Found Event", () => Browser.Exists(By.Id("test-info")).Text);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanRenderNotFoundPageNoStreaming(bool useCustomNotFoundPage)
    {
        string query = useCustomNotFoundPage ? "&useCustomNotFoundPage=true" : "";
        Navigate($"{ServerPathBase}/set-not-found?shouldSet=true{query}");

        if (useCustomNotFoundPage)
        {
            var infoText = Browser.FindElement(By.Id("test-info")).Text;
            Assert.Contains("Welcome On Custom Not Found Page", infoText);
            // custom page should have a custom layout
            var aboutLink = Browser.FindElement(By.Id("about-link")).Text;
            Assert.Contains("About", aboutLink);
        }
        else
        {
            var bodyText = Browser.FindElement(By.TagName("body")).Text;
            Assert.Contains("There's nothing here", bodyText);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanRenderNotFoundPageWithStreaming(bool useCustomNotFoundPage)
    {
        // when streaming started, we always render page under "not-found" path
        string query = useCustomNotFoundPage ? "?useCustomNotFoundPage=true" : "";
        Navigate($"{ServerPathBase}/streaming-set-not-found{query}");

        string expectedTitle = "Default Not Found Page";
        Browser.Equal(expectedTitle, () => Browser.Title);
    }
}
