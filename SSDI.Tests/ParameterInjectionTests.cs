using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSDI.Parameters;

namespace SSDI.Tests;

[TestClass]
public class ParameterInjectionTests
{
    #region Registration-time Parameters

    [TestMethod]
    public void WithCtorParam_TypedParameter_InjectsValue()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam("TestName")
                .WithCtorParam(8080));

        // Act
        var result = container.Locate<ServiceWithParameters>();

        // Assert
        Assert.AreEqual("TestName", result.Name);
        Assert.AreEqual(8080, result.Port);
    }

    [TestMethod]
    public void WithCtorParam_NamedParameter_InjectsValue()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam("name", "TestName")
                .WithCtorParam("port", 9090));

        // Act
        var result = container.Locate<ServiceWithParameters>();

        // Assert
        Assert.AreEqual("TestName", result.Name);
        Assert.AreEqual(9090, result.Port);
    }

    [TestMethod]
    public void WithCtorParam_PositionalParameter_InjectsValue()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam(0, "PositionalName")
                .WithCtorParam(1, 7070));

        // Act
        var result = container.Locate<ServiceWithParameters>();

        // Assert
        Assert.AreEqual("PositionalName", result.Name);
        Assert.AreEqual(7070, result.Port);
    }

    [TestMethod]
    public void WithCtorPositionalParams_MultipleParameters_InjectsAllValues()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorPositionalParams("MultiName", 6060));

        // Act
        var result = container.Locate<ServiceWithParameters>();

        // Assert
        Assert.AreEqual("MultiName", result.Name);
        Assert.AreEqual(6060, result.Port);
    }

    [TestMethod]
    public void WithCtorParam_MixedWithDependencies_ResolvesAll()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<ServiceWithMixedParameters>()
                .WithCtorParam("Server=localhost")
                .WithCtorParam(30);
        });

        // Act
        var result = container.Locate<ServiceWithMixedParameters>();

        // Assert
        Assert.IsNotNull(result.Dependency);
        Assert.AreEqual("Server=localhost", result.ConnectionString);
        Assert.AreEqual(30, result.Timeout);
    }

    [TestMethod]
    public void Locate_WithLegacyNamedParameters_InjectsValues()
    {
        // Arrange - use runtime parameters with legacy interface
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithParameters>());

        // Act
        var result = container.Locate<ServiceWithParameters>(
            new NamedParameter("name", "LegacyName"),
            new NamedParameter("port", 5050));

        // Assert
        Assert.AreEqual("LegacyName", result.Name);
        Assert.AreEqual(5050, result.Port);
    }

    [TestMethod]
    public void Locate_WithLegacyTypedParameters_InjectsValues()
    {
        // Arrange - use runtime parameters with legacy interface
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithParameters>());

        // Act
        var result = container.Locate<ServiceWithParameters>(
            new TypedParameter("LegacyTyped"),
            new TypedParameter(6060));

        // Assert
        Assert.AreEqual("LegacyTyped", result.Name);
        Assert.AreEqual(6060, result.Port);
    }

    #endregion

    #region Runtime Parameters

    [TestMethod]
    public void LocateWithPositionalParams_InjectsParameters()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithParameters>());

        // Act
        var result = container.LocateWithPositionalParams<ServiceWithParameters>("RuntimeName", 4040);

        // Assert
        Assert.AreEqual("RuntimeName", result.Name);
        Assert.AreEqual(4040, result.Port);
    }

    [TestMethod]
    public void LocateWithNamedParameters_InjectsParameters()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithParameters>());

        // Act
        var result = container.LocateWithNamedParameters<ServiceWithParameters>(
            ("name", "NamedRuntime"),
            ("port", 3030));

        // Assert
        Assert.AreEqual("NamedRuntime", result.Name);
        Assert.AreEqual(3030, result.Port);
    }

    [TestMethod]
    public void LocateWithTypedParams_InjectsParameters()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithParameters>());

        // Act
        var result = container.LocateWithTypedParams<ServiceWithParameters>("TypedRuntime", 2020);

        // Assert
        Assert.AreEqual("TypedRuntime", result.Name);
        Assert.AreEqual(2020, result.Port);
    }

    [TestMethod]
    public void Locate_WithDIParameterArray_InjectsParameters()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithParameters>());

        // Act
        var result = container.Locate<ServiceWithParameters>(
            DIParameter.Named("name", "DIParamName"),
            DIParameter.Positional(1, 1010));

        // Assert
        Assert.AreEqual("DIParamName", result.Name);
        Assert.AreEqual(1010, result.Port);
    }

    [TestMethod]
    public void Locate_WithSinglePositionalParameter_InjectsParameter()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam(1, 8080)); // Register port at position 1

        // Act
        var result = container.Locate<ServiceWithParameters>(0, "SinglePos");

        // Assert
        Assert.AreEqual("SinglePos", result.Name);
        Assert.AreEqual(8080, result.Port);
    }

    [TestMethod]
    public void LocateWithParams_LegacyInterface_InjectsParameters()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithParameters>());

        // Act
        var result = container.LocateWithParams<ServiceWithParameters>(
            new TypedParameter("LegacyTyped"),
            new TypedParameter(999));

        // Assert
        Assert.AreEqual("LegacyTyped", result.Name);
        Assert.AreEqual(999, result.Port);
    }

    #endregion

    #region Optional Parameters

    [TestMethod]
    public void OptionalParameter_NotProvided_UsesDefault()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithOptionalParameter>()
                .WithCtorParam("name", "TestName"));

        // Act
        var result = container.Locate<ServiceWithOptionalParameter>();

        // Assert
        Assert.AreEqual("TestName", result.Name);
        Assert.AreEqual(8080, result.Port); // Default value
    }

    [TestMethod]
    public void OptionalParameter_Provided_UsesProvidedValue()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithOptionalParameter>()
                .WithCtorParam("name", "TestName")
                .WithCtorParam("port", 9999));

        // Act
        var result = container.Locate<ServiceWithOptionalParameter>();

        // Assert
        Assert.AreEqual("TestName", result.Name);
        Assert.AreEqual(9999, result.Port);
    }

    #endregion

    #region Parameter Types Tests

    [TestMethod]
    public void DIParameter_Named_MatchesByName()
    {
        // Arrange
        var param = DIParameter.Named("testParam", "value");

        // Act & Assert
        Assert.IsTrue(param.Matches("testParam", 0, typeof(string)));
        Assert.IsFalse(param.Matches("otherParam", 0, typeof(string)));
    }

    [TestMethod]
    public void DIParameter_Positional_MatchesByPosition()
    {
        // Arrange
        var param = DIParameter.Positional(2, "value");

        // Act & Assert
        Assert.IsTrue(param.Matches("anyName", 2, typeof(string)));
        Assert.IsFalse(param.Matches("anyName", 0, typeof(string)));
    }

    [TestMethod]
    public void DIParameter_Typed_MatchesByType()
    {
        // Arrange
        var param = DIParameter.Typed("value");

        // Act & Assert
        Assert.IsTrue(param.Matches("anyName", 0, typeof(string)));
        Assert.IsFalse(param.Matches("anyName", 0, typeof(int)));
    }

    [TestMethod]
    public void DIParameter_Typed_MatchesAssignableTypes()
    {
        // Arrange
        var param = DIParameter.Typed(new ServiceImplementation());

        // Act & Assert
        Assert.IsTrue(param.Matches("anyName", 0, typeof(IService)));
        Assert.IsTrue(param.Matches("anyName", 0, typeof(ServiceImplementation)));
    }

    [TestMethod]
    public void NamedParameter_LegacyClass_Works()
    {
        // Arrange
        var param = new NamedParameter("test", "value");

        // Act & Assert
        Assert.AreEqual("test", param.Name);
        Assert.AreEqual("value", param.Value);
    }

    [TestMethod]
    public void PositionalParameter_LegacyClass_Works()
    {
        // Arrange
        var param = new PositionalParameter(3, 42);

        // Act & Assert
        Assert.AreEqual(3, param.Position);
        Assert.AreEqual(42, param.Value);
    }

    [TestMethod]
    public void TypedParameter_LegacyClass_Works()
    {
        // Arrange
        var param = new TypedParameter("testValue");

        // Act & Assert
        Assert.AreEqual("testValue", param.Value);
        Assert.AreEqual(typeof(string), param.ValueType);
    }

    #endregion

    #region Combined Registration and Runtime Parameters

    [TestMethod]
    public void CombinedParams_RegisterFirstParam_LocateSecondParam()
    {
        // Arrange - Register host at registration time, provide port at locate time
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam(0, "localhost")); // Register name at position 0

        // Act - Provide port at locate time
        var result = container.Locate<ServiceWithParameters>(
            DIParameter.Positional(1, 9090));

        // Assert
        Assert.AreEqual("localhost", result.Name);
        Assert.AreEqual(9090, result.Port);
    }

    [TestMethod]
    public void CombinedParams_RegisterSecondParam_LocateFirstParam()
    {
        // Arrange - Register port at registration time, provide name at locate time
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam(1, 8080)); // Register port at position 1

        // Act - Provide name at locate time
        var result = container.Locate<ServiceWithParameters>(
            DIParameter.Positional(0, "runtime-host"));

        // Assert
        Assert.AreEqual("runtime-host", result.Name);
        Assert.AreEqual(8080, result.Port);
    }

    [TestMethod]
    public void CombinedParams_RegisterFirstTwo_LocateThird()
    {
        // Arrange - Register host and port, provide useSsl at runtime
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithThreeParameters>()
                .WithCtorParam(0, "example.com")
                .WithCtorParam(1, 443));

        // Act
        var result = container.Locate<ServiceWithThreeParameters>(
            DIParameter.Positional(2, true));

        // Assert
        Assert.AreEqual("example.com", result.Host);
        Assert.AreEqual(443, result.Port);
        Assert.IsTrue(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_RegisterMiddleParam_LocateFirstAndLast()
    {
        // Arrange - Register only the middle parameter (port)
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithThreeParameters>()
                .WithCtorParam(1, 8080));

        // Act - Provide first and last at runtime
        var result = container.Locate<ServiceWithThreeParameters>(
            DIParameter.Positional(0, "api.server.com"),
            DIParameter.Positional(2, false));

        // Assert
        Assert.AreEqual("api.server.com", result.Host);
        Assert.AreEqual(8080, result.Port);
        Assert.IsFalse(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_RegisterByName_LocateByPosition()
    {
        // Arrange - Register host by name
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithThreeParameters>()
                .WithCtorParam("host", "named-host"));

        // Act - Provide port and useSsl by position
        var result = container.Locate<ServiceWithThreeParameters>(
            DIParameter.Positional(1, 3000),
            DIParameter.Positional(2, true));

        // Assert
        Assert.AreEqual("named-host", result.Host);
        Assert.AreEqual(3000, result.Port);
        Assert.IsTrue(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_RegisterByPosition_LocateByName()
    {
        // Arrange - Register host by position
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithThreeParameters>()
                .WithCtorParam(0, "positional-host"));

        // Act - Provide port and useSsl by name
        var result = container.Locate<ServiceWithThreeParameters>(
            DIParameter.Named("port", 5000),
            DIParameter.Named("useSsl", false));

        // Assert
        Assert.AreEqual("positional-host", result.Host);
        Assert.AreEqual(5000, result.Port);
        Assert.IsFalse(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_RegisterByType_LocateMissingByPosition()
    {
        // Arrange - Register bool by type (useSsl)
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithThreeParameters>()
                .WithCtorParam(true)); // bool matches useSsl

        // Act - Provide host and port at runtime
        var result = container.Locate<ServiceWithThreeParameters>(
            DIParameter.Positional(0, "typed-host"),
            DIParameter.Positional(1, 7777));

        // Assert
        Assert.AreEqual("typed-host", result.Host);
        Assert.AreEqual(7777, result.Port);
        Assert.IsTrue(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_WithDependency_RegisterSomeParams_LocateOthers()
    {
        // Arrange - Register dependency and host, provide port and useSsl at runtime
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<ServiceWithDependencyAndThreeParameters>()
                .WithCtorParam("host", "injected-host");
        });

        // Act
        var result = container.Locate<ServiceWithDependencyAndThreeParameters>(
            DIParameter.Named("port", 6060),
            DIParameter.Named("useSsl", true));

        // Assert
        Assert.IsNotNull(result.Dependency);
        Assert.AreEqual("injected-host", result.Host);
        Assert.AreEqual(6060, result.Port);
        Assert.IsTrue(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_FourParams_RegisterTwoAlternating_LocateOtherTwo()
    {
        // Arrange - Register host (0) and username (2)
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithFourParameters>()
                .WithCtorParam(0, "db.server.com")
                .WithCtorParam(2, "admin"));

        // Act - Provide port (1) and password (3)
        var result = container.Locate<ServiceWithFourParameters>(
            DIParameter.Positional(1, 5432),
            DIParameter.Positional(3, "secret123"));

        // Assert
        Assert.AreEqual("db.server.com", result.Host);
        Assert.AreEqual(5432, result.Port);
        Assert.AreEqual("admin", result.Username);
        Assert.AreEqual("secret123", result.Password);
    }

    [TestMethod]
    public void CombinedParams_RegisteredParamsTakePrecedence()
    {
        // Arrange - Register all params at registration time
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam(0, "registered-name")
                .WithCtorParam(1, 1111));

        // Act - Try to override name at runtime
        var result = container.Locate<ServiceWithParameters>(
            DIParameter.Positional(0, "runtime-override"));

        // Assert - registered params take precedence over runtime params
        Assert.AreEqual("registered-name", result.Name);
        Assert.AreEqual(1111, result.Port);
    }

    [TestMethod]
    public void CombinedParams_LocateWithPositionalParams_FillsGaps()
    {
        // Arrange - Register middle param
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithThreeParameters>()
                .WithCtorParam(1, 9999)); // port in the middle

        // Act - Use LocateWithPositionalParams (starts at position 0)
        // This should fill position 0 and 2
        var result = container.LocateWithPositionalParams<ServiceWithThreeParameters>(
            "positional-host", 9999, true);

        // Assert
        Assert.AreEqual("positional-host", result.Host);
        Assert.AreEqual(9999, result.Port); // Could be either - runtime wins
        Assert.IsTrue(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_LocateWithTypedParams_FillsMissingTypes()
    {
        // Arrange - Register the int param (port)
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithThreeParameters>()
                .WithCtorParam(8080)); // int type matches port

        // Act - Provide string (host) and bool (useSsl) at runtime
        var result = container.LocateWithTypedParams<ServiceWithThreeParameters>(
            "typed-host", false);

        // Assert
        Assert.AreEqual("typed-host", result.Host);
        Assert.AreEqual(8080, result.Port);
        Assert.IsFalse(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_LocateWithNamedParams_FillsMissingNames()
    {
        // Arrange - Register host
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithThreeParameters>()
                .WithCtorParam("host", "registered-host"));

        // Act - Provide port and useSsl by name
        var result = container.LocateWithNamedParameters<ServiceWithThreeParameters>(
            ("port", 4040),
            ("useSsl", true));

        // Assert
        Assert.AreEqual("registered-host", result.Host);
        Assert.AreEqual(4040, result.Port);
        Assert.IsTrue(result.UseSsl);
    }

    [TestMethod]
    public void CombinedParams_SingletonWithRuntimeParams_FirstCallWins()
    {
        // Arrange - Register as singleton with one param
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam(0, "singleton-name")
                .Lifestyle.Singleton());

        // Act - First locate with runtime param
        var result1 = container.Locate<ServiceWithParameters>(
            DIParameter.Positional(1, 1111));

        // Second locate with different runtime param
        var result2 = container.Locate<ServiceWithParameters>(
            DIParameter.Positional(1, 2222));

        // Assert - Both should be same instance (singleton)
        Assert.AreSame(result1, result2);
        Assert.AreEqual("singleton-name", result1.Name);
        Assert.AreEqual(1111, result1.Port); // First call's value wins
    }

    #endregion

    #region Multiple Constructors

    [TestMethod]
    public void MultipleConstructors_NoParams_UsesDefaultConstructor()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithMultipleConstructors>());

        // Act
        var result = container.Locate<ServiceWithMultipleConstructors>();

        // Assert
        Assert.IsTrue(result.UsedDefaultConstructor);
    }

    [TestMethod]
    public void MultipleConstructors_OneParam_UsesMatchingConstructor()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithMultipleConstructors>()
                .WithCtorParam("TestName"));

        // Act
        var result = container.Locate<ServiceWithMultipleConstructors>();

        // Assert
        Assert.IsFalse(result.UsedDefaultConstructor);
        Assert.AreEqual("TestName", result.Name);
        Assert.AreEqual(0, result.Port);
    }

    [TestMethod]
    public void MultipleConstructors_TwoParams_UsesMatchingConstructor()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithMultipleConstructors>()
                .WithCtorParam("TestName")
                .WithCtorParam(9999));

        // Act
        var result = container.Locate<ServiceWithMultipleConstructors>();

        // Assert
        Assert.IsFalse(result.UsedDefaultConstructor);
        Assert.AreEqual("TestName", result.Name);
        Assert.AreEqual(9999, result.Port);
    }

    #endregion
}
