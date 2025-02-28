using TestStack.ConventionTests;
using TestStack.ConventionTests.ConventionData;

namespace Shouldly.Tests.ConventionTests;

public class ShouldlyConventions
{
    private readonly Types _shouldlyMethodClasses;

    public ShouldlyConventions()
    {
        _shouldlyMethodClasses = Types.InAssemblyOf<ShouldAssertException>(
            "Shouldly extension classes",
            t => t.HasAttribute("Shouldly.ShouldlyMethodsAttribute"));
    }

#if false
        [Fact]
        [UseCulture("en-US")]
        public void ShouldHaveCustomMessageOverloads()
        {
            Convention.GetFailures(new ShouldlyMethodsShouldHaveCustomMessageOverload(), _shouldlyMethodClasses)
                .ShouldMatchApproved();
        }
#endif

    [Fact]
    public void VerifyItWorks()
    {
        var ex = Should.Throw<ConventionFailedException>(() =>
        {
            var convention = new ShouldlyMethodsShouldHaveCustomMessageOverload();
            var types = Types.InCollection([typeof(TestWithMissingOverloads)], "Sample");
            Convention.Is(convention, types);
        });

        ex.Message.ShouldContain("ShouldAlsoFail");
    }

    [Fact]
    public void ShouldThrowMethodsShouldHaveExtensions()
    {
        Convention.Is(new ShouldThrowMatchesExtensionsConvention(), _shouldlyMethodClasses);
    }

    [Fact]
    public void MethodsShouldNotBeInlined()
    {
        Convention.Is(new MethodsShouldNotBeInlinedConvention(), _shouldlyMethodClasses);
    }

    [Fact]
    public void ExtensionClassesShouldNotBeBrowsable()
    {
        Convention.Is(new ExtensionMethodsShouldNotBeBrowsableConvention(), _shouldlyMethodClasses);
    }
}

public static class TestWithMissingOverloads
{
    public static void ShouldTest(this object foo) { }

    public static void ShouldAlsoFail(this object foo) { }
    public static void ShouldAlsoFail(this object foo, string customMessage) { }
    public static void ShouldAlsoFail(this object foo, int param) { }
    public static void ShouldAlsoFail(this object foo, int param, string customMessage) { }
}