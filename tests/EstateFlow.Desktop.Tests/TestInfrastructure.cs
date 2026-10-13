using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;

namespace EstateFlow.Desktop.Tests;

// A fake for any interface: async methods complete immediately with a value
// registered via Returns(), or default. Records every call.
public class Fake<T> : DispatchProxy where T : class
{
    private readonly ConcurrentDictionary<string, object?> _results = new();

    public ConcurrentQueue<(string Method, object?[] Args)> Calls { get; } = new();

    public T Object => (T)(object)this;

    public static Fake<T> Create() => (Fake<T>)(object)DispatchProxy.Create<T, Fake<T>>();

    public Fake<T> Returns(string method, object? result)
    {
        _results[method] = result;
        return this;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        Calls.Enqueue((method!.Name, args ?? Array.Empty<object?>()));
        _results.TryGetValue(method.Name, out var result);

        var returnType = method.ReturnType;

        if (returnType == typeof(Task))
            return Task.CompletedTask;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var valueType = returnType.GetGenericArguments()[0];
            var value = result ?? DefaultFor(valueType);
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(valueType).Invoke(null, new[] { value });
        }

        return result ?? DefaultFor(returnType);
    }

    private static object? DefaultFor(Type type)
    {
        if (type == typeof(void)) return null;
        if (type.IsValueType) return Activator.CreateInstance(type);
        if (type == typeof(string)) return string.Empty;

        // Empty lists rather than null, like the real client.
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return Activator.CreateInstance(type);

        return null;
    }
}

// WPF needs a single STA thread with a dispatcher and one Application.
// All UI tests run their bodies here, one at a time.
public static class Wpf
{
    private static readonly Lazy<Dispatcher> UiDispatcher = new(Start);

    public static readonly BindingErrorListener BindingErrors = new();

    public static void Run(Action action) =>
        UiDispatcher.Value.Invoke(action);

    public static T Run<T>(Func<T> func) =>
        UiDispatcher.Value.Invoke(func);

    // Lets queued async continuations (e.g. view-model loads) finish.
    public static void DoEvents() =>
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

    private static Dispatcher Start()
    {
        Dispatcher? dispatcher = null;
        using var ready = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;

            // Loads App.xaml: theme, converters and view-model → view templates.
            var app = new App { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();

            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(BindingErrors);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            ready.Set();
            Dispatcher.Run();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        ready.Wait();

        return dispatcher!;
    }
}

// Collects WPF binding errors (e.g. a misspelt property name in XAML), which
// WPF otherwise only writes to the debug output.
public class BindingErrorListener : TraceListener
{
    private readonly List<string> _errors = new();

    public IReadOnlyList<string> Errors
    {
        get { lock (_errors) return _errors.ToList(); }
    }

    public void Clear()
    {
        lock (_errors) _errors.Clear();
    }

    public override void Write(string? message) { }

    public override void WriteLine(string? message)
    {
        if (message is null)
            return;

        lock (_errors) _errors.Add(message);
    }
}
