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
