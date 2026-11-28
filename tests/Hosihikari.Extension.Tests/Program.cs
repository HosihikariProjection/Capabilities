using Hosihikari.Extension.Abstractions;
using Hosihikari.Extension.Async;
using Hosihikari.Extension.Event;
using Hosihikari.Extension.Hook;
using Hosihikari.Extension.UI;

var failures = 0;

void Check(string name, bool condition)
{
    if (condition)
    {
        return;
    }

    failures++;
    Console.Error.WriteLine($"FAIL {name}");
}

bool Throws<T>(Action action)
    where T : Exception
{
    try
    {
        action();
        return false;
    }
    catch (T)
    {
        return true;
    }
    catch
    {
        return false;
    }
}

// Availability starts off in a process that carries no adapter.
Check("event starts unavailable", !EventExtension.IsAvailable);
Check("async starts unavailable", !AsyncExtension.IsAvailable);
Check("hook starts unavailable", !HookExtension.IsAvailable);
Check("ui starts unavailable", !UiExtension.IsAvailable);

// The unavailable path is loud and names the capability.
Check(
    "async service throws",
    Throws<CarrierCapabilityUnavailableException>(() => _ = AsyncExtension.Service)
);
Check(
    "async post throws",
    Throws<CarrierCapabilityUnavailableException>(() => AsyncExtension.Post(() => { }))
);
Check(
    "async run throws",
    Throws<CarrierCapabilityUnavailableException>(() => AsyncExtension.RunAsync(() => { }))
);
Check(
    "hook service throws",
    Throws<CarrierCapabilityUnavailableException>(() => _ = HookExtension.Service)
);
Check(
    "hook attach throws",
    Throws<CarrierCapabilityUnavailableException>(() => HookExtension.Attach(0x1000, 0x2000))
);
Check(
    "ui service throws",
    Throws<CarrierCapabilityUnavailableException>(() => _ = UiExtension.Service)
);
Check(
    "ui show throws",
    Throws<CarrierCapabilityUnavailableException>(() =>
        UiExtension.ShowActionForm(new FakePlayer(), new ActionFormDefinition("t", "c", []))
    )
);

// The same exception type is what a carrier that lacks a named capability reports.
var unavailable = new CarrierCapabilityUnavailableException("Command", "Endstone");
Check("exception is a NotSupportedException", unavailable is NotSupportedException);
Check("exception keeps the capability", unavailable.Capability == "Command");
Check("exception keeps the carrier", unavailable.Carrier == "Endstone");
Check(
    "exception names both",
    unavailable.Message.Contains("Command") && unavailable.Message.Contains("Endstone")
);

// Event shapes: cancellation and the writable chat message are the plugin-facing contract.
var chat = new PlayerChatEvent(new FakePlayer(), "hello");
Check("chat keeps its message", chat.Message == "hello");
Check("chat starts uncancelled", !chat.IsCancelled);
chat.Message = "edited";
Check("chat message is writable", chat.Message == "edited");
chat.Cancel();
Check("chat cancels", chat.IsCancelled);
Check("chat rejects a null message", Throws<ArgumentNullException>(() => chat.Message = null!));
Check(
    "chat rejects a null player",
    Throws<ArgumentNullException>(() => new PlayerChatEvent(null!, "m"))
);
Check(
    "chat rejects a null initial message",
    Throws<ArgumentNullException>(() => new PlayerChatEvent(new FakePlayer(), null!))
);
Check(
    "join rejects a null player",
    Throws<ArgumentNullException>(() => new PlayerJoinEvent(null!))
);
Check(
    "leave rejects a null player",
    Throws<ArgumentNullException>(() => new PlayerLeaveEvent(null!))
);
Check("join keeps its player", new PlayerJoinEvent(new FakePlayer()).Player.Name == "Steve");

// The carrier marks the event capability available once its listeners are attached.
EventExtension.MarkAvailable();
Check("event is available after the mark", EventExtension.IsAvailable);

// Async: the adapter's service becomes the entry point's target.
var async = new FakeAsyncService();
AsyncExtension.Use(async);
Check("async is available after Use", AsyncExtension.IsAvailable);
Check("async service is the registered one", ReferenceEquals(AsyncExtension.Service, async));

var posted = 0;
AsyncExtension.Post(() => posted++);
Check("post reaches the service", posted == 1);

var fault = AsyncExtension.RunAsync(() => throw new InvalidOperationException("boom"));
Check(
    "run surfaces the work's exception",
    fault.IsFaulted && fault.Exception!.InnerException!.Message == "boom"
);

// The awaitable always yields and posts its continuation to the server thread.
var awaiter = AsyncExtension.SwitchToServerThread().GetAwaiter();
Check("awaitable always yields", !awaiter.IsCompleted);
awaiter.OnCompleted(() => posted += 10);
Check("awaitable posts the continuation", async.PostCount == 3 && posted == 11);

var resumed = false;
await AsyncExtension.SwitchToServerThread();
resumed = true;
Check("await resumes through the service", resumed && async.PostCount == 4);

// Hook: the adapter's service receives the raw addresses and the options.
var hooks = new FakeHookService();
HookExtension.Use(hooks);
Check("hook is available after Use", HookExtension.IsAvailable);
var hook = HookExtension.Attach(0x1000, 0x2000, HookPriority.High, suspendThreads: false);
Check("hook forwards the target", hooks.Target == 0x1000);
Check("hook forwards the detour", hooks.Detour == 0x2000);
Check("hook forwards the priority", hooks.Priority == HookPriority.High);
Check("hook forwards thread suspension", !hooks.SuspendThreads);
Check("hook returns the installed hook", hook.IsInstalled && hook.Target == 0x1000);
hook.Dispose();
Check("disposing uninstalls", !hook.IsInstalled);

// UI: the form definition and both callbacks reach the adapter unchanged.
var ui = new FakeUiService();
UiExtension.Use(ui);
Check("ui is available after Use", UiExtension.IsAvailable);
var form = new ActionFormDefinition(
    "Pick",
    "Choose one",
    [new ActionButtonDefinition("One"), new ActionButtonDefinition("Two", "textures/items/apple")]
);
ActionFormResponse? chosen = null;
var closed = false;
var player = new FakePlayer();
UiExtension.ShowActionForm(player, form, response => chosen = response, () => closed = true);
Check("ui forwards the player", ReferenceEquals(ui.Player, player));
Check("ui forwards the form", ReferenceEquals(ui.Form, form));
ui.Submit!.Invoke(new ActionFormResponse(1));
ui.Close!.Invoke();
Check("ui forwards the submit callback", chosen is { ButtonIndex: 1 });
Check("ui forwards the close callback", closed);

Console.WriteLine(failures == 0 ? "OK" : $"{failures} failure(s)");
return failures == 0 ? 0 : 1;

sealed class FakePlayer : IPlayerView
{
    public string Name => "Steve";

    public string Xuid => "";

    public object? NativeView => null;
}

sealed class FakeAsyncService : IAsyncService
{
    public int PostCount { get; private set; }

    public bool IsAvailable => true;

    public bool HasSynchronizationContext => false;

    public void Post(Action work)
    {
        PostCount++;
        work();
    }

    public Task RunAsync(Action work)
    {
        PostCount++;
        try
        {
            work();
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}

sealed class FakeHookService : IHookService
{
    public nint Target { get; private set; }

    public nint Detour { get; private set; }

    public HookPriority Priority { get; private set; }

    public bool SuspendThreads { get; private set; }

    public bool IsAvailable => true;

    public IHook Attach(
        nint target,
        nint detour,
        HookPriority priority = HookPriority.Normal,
        bool suspendThreads = true
    )
    {
        Target = target;
        Detour = detour;
        Priority = priority;
        SuspendThreads = suspendThreads;
        return new FakeHook(target);
    }
}

sealed class FakeHook(nint target) : IHook
{
    public nint Target { get; } = target;

    public nint Detour => 0;

    public nint Original => 0;

    public bool IsInstalled { get; private set; } = true;

    public void Dispose() => IsInstalled = false;
}

sealed class FakeUiService : IUiService
{
    public IPlayerView? Player { get; private set; }

    public ActionFormDefinition? Form { get; private set; }

    public Action<ActionFormResponse>? Submit { get; private set; }

    public Action? Close { get; private set; }

    public bool IsAvailable => true;

    public void ShowActionForm(
        IPlayerView player,
        ActionFormDefinition form,
        Action<ActionFormResponse>? onSubmit = null,
        Action? onClose = null
    )
    {
        Player = player;
        Form = form;
        Submit = onSubmit;
        Close = onClose;
    }
}
