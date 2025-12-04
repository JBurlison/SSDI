namespace SSDI.Benchmarks.Classes;

// Dummy services (10 simple transients)
public interface IDummy1 { }
public interface IDummy2 { }
public interface IDummy3 { }
public interface IDummy4 { }
public interface IDummy5 { }
public interface IDummy6 { }
public interface IDummy7 { }
public interface IDummy8 { }
public interface IDummy9 { }
public interface IDummy10 { }

public class Dummy1 : IDummy1 { }
public class Dummy2 : IDummy2 { }
public class Dummy3 : IDummy3 { }
public class Dummy4 : IDummy4 { }
public class Dummy5 : IDummy5 { }
public class Dummy6 : IDummy6 { }
public class Dummy7 : IDummy7 { }
public class Dummy8 : IDummy8 { }
public class Dummy9 : IDummy9 { }
public class Dummy10 : IDummy10 { }

// Standard services (transient, singleton, combined)
public interface ISingleton { int Id { get; } }
public interface ITransient { int Id { get; } }
public interface ICombined { ISingleton Singleton { get; } ITransient Transient { get; } }

public class Singleton : ISingleton
{
    private static int _counter;
    public int Id { get; } = Interlocked.Increment(ref _counter);
}

public class Transient : ITransient
{
    private static int _counter;
    public int Id { get; } = Interlocked.Increment(ref _counter);
}

public class Combined : ICombined
{
    public ISingleton Singleton { get; }
    public ITransient Transient { get; }

    public Combined(ISingleton singleton, ITransient transient)
    {
        Singleton = singleton;
        Transient = transient;
    }
}

// Complex services (deep dependency graph)
public interface IFirstService { }
public interface ISecondService { }
public interface IThirdService { }

public class FirstService : IFirstService { }
public class SecondService : ISecondService { }
public class ThirdService : IThirdService { }

public interface ISubObject1 { }
public interface ISubObject2 { }
public interface ISubObject3 { }

public class SubObject1 : ISubObject1
{
    public IFirstService FirstService { get; }
    public SubObject1(IFirstService firstService) => FirstService = firstService;
}

public class SubObject2 : ISubObject2
{
    public ISecondService SecondService { get; }
    public SubObject2(ISecondService secondService) => SecondService = secondService;
}

public class SubObject3 : ISubObject3
{
    public IThirdService ThirdService { get; }
    public SubObject3(IThirdService thirdService) => ThirdService = thirdService;
}

public interface IComplex { }

public class Complex : IComplex
{
    public IFirstService FirstService { get; }
    public ISecondService SecondService { get; }
    public IThirdService ThirdService { get; }
    public ISubObject1 SubObject1 { get; }
    public ISubObject2 SubObject2 { get; }
    public ISubObject3 SubObject3 { get; }

    public Complex(
        IFirstService firstService,
        ISecondService secondService,
        IThirdService thirdService,
        ISubObject1 subObject1,
        ISubObject2 subObject2,
        ISubObject3 subObject3)
    {
        FirstService = firstService;
        SecondService = secondService;
        ThirdService = thirdService;
        SubObject1 = subObject1;
        SubObject2 = subObject2;
        SubObject3 = subObject3;
    }
}
