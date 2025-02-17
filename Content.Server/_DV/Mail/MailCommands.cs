using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Administration;
using Content.Server.Administration;
using Content.Server._DV.Mail.Components;
using Content.Server._DV.Mail.EntitySystems;
using Content.Server._NF.SectorServices;
using Content.Server._DV.Cargo.Components;
using Content.Server._NF.Mail.Components;

namespace Content.Server._DV.Mail;

[AdminCommand(AdminFlags.Fun)]
public sealed class MailToCommand : LocalizedCommands // Frontier: IConsoleCommand < LocalizedCommands
{
    public override string Command => "mailto"; // Frontier: add override
    public override string Description => Loc.GetString("command-mailto-description", ("requiredComponent", nameof(MailReceiverComponent))); // Frontier: add override
    public override string Help => Loc.GetString("command-mailto-help", ("command", Command)); // Frontier: add override

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private const string BlankMailPrototype = "MailAdminFun";
    private const string BlankLargeMailPrototype = "MailLargeAdminFun"; // Frontier: large mail
    private const string Container = "storagebase";
    private const string MailContainer = "contents";


    public override void Execute(IConsoleShell shell, string argStr, string[] args) // Frontier: async < override
    {
        if (args.Length < 2) // Frontier: 4<2 - optional arguments
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var recipientUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!EntityUid.TryParse(args[1], out var containerUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        // Frontier: optional arguments, large mail
        var isFragile = false;
        if (args.Length > 2 && !bool.TryParse(args[2], out isFragile))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        var isPriority = false;
        if (args.Length > 3 && !bool.TryParse(args[3], out isPriority))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        var isLarge = false;
        if (args.Length > 4 && !bool.TryParse(args[4], out isLarge))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }
        var mailPrototype = isLarge ? BlankLargeMailPrototype : BlankMailPrototype;
        // End Frontier

        var mailSystem = _entitySystemManager.GetEntitySystem<MailSystem>();
        var containerSystem = _entitySystemManager.GetEntitySystem<SharedContainerSystem>();
        var sectorService = _entitySystemManager.GetEntitySystem<SectorServiceSystem>(); // Frontier

        // Frontier: sector-wide mail
        if (!_entityManager.TryGetComponent(sectorService.GetServiceEntity(), out SectorMailComponent? sectorMail))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-mailservice"));
            return;
        }
        // End Frontier

        if (!_entityManager.HasComponent<MailReceiverComponent>(recipientUid))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-mailreceiver", ("requiredComponent", nameof(MailReceiverComponent))));
            return;
        }

        if (!_prototypeManager.HasIndex<EntityPrototype>(mailPrototype)) // Frontier: _blankMailPrototype<mailPrototype
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-blankmail", ("blankMail", mailPrototype))); // Frontier: _blankMailPrototype<mailPrototype
            return;
        }

        // Frontier: box optional
        // if (!containerSystem.TryGetContainer(containerUid, Container, out var targetContainer))
        // {
        //     shell.WriteLine(Loc.GetString("command-mailto-invalid-container", ("requiredContainer", Container)));
        //     return;
        // }
        // End Frontier

        if (!mailSystem.TryGetMailRecipientForReceiver(recipientUid, out var recipient))
        {
            shell.WriteLine(Loc.GetString("command-mailto-unable-to-receive"));
            return;
        }

        if (!mailSystem.TryGetMailTeleporterForReceiver(recipientUid, out var teleporterComponent, out var teleporterUid))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-teleporter-found"));
            return;
        }

        var mailUid = _entityManager.SpawnEntity(mailPrototype, _entityManager.GetComponent<TransformComponent>(containerUid).Coordinates); // Frontier: _blankMailPrototype<mailPrototype
        var mailContents = containerSystem.EnsureContainer<Container>(mailUid, MailContainer);

        if (!_entityManager.TryGetComponent<MailComponent>(mailUid, out var mailComponent))
        {
            shell.WriteLine(Loc.GetString("command-mailto-bogus-mail", ("blankMail", mailPrototype), ("requiredMailComponent", nameof(MailComponent)))); // Frontier: _blankMailPrototype<mailPrototype
            return;
        }

        // Frontier: box optional
        if (containerSystem.TryGetContainer(containerUid, Container, out var targetContainer))
        {
            foreach (var entity in targetContainer.ContainedEntities.ToArray())
            {
                containerSystem.Insert(entity, mailContents);
            }
        }
        else
        {
            containerSystem.Insert(containerUid, mailContents);
        }
        // End Frontier

        mailComponent.IsFragile = isFragile;
        mailComponent.IsPriority = isPriority;
        mailComponent.IsLarge = isLarge;

        mailSystem.SetupMail(mailUid, sectorMail, recipient.Value); // Frontier: use SectorMailComponent

        var teleporterQueue = containerSystem.EnsureContainer<Container>((EntityUid)teleporterUid, "queued");
        containerSystem.Insert(mailUid, teleporterQueue);
        shell.WriteLine(Loc.GetString("command-mailto-success", ("timeToTeleport", sectorMail.TeleportInterval.TotalSeconds - sectorMail.Accumulator))); // Frontier: use SectorMailComponent
    }

    // Frontier: completion
    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHint(Loc.GetString("command-mailto-completion-recipient"));
            case 2:
                return CompletionResult.FromHint(Loc.GetString("command-mailto-completion-container"));
            case 3:
                return CompletionResult.FromHint(Loc.GetString("command-mailto-completion-fragile"));
            case 4:
                return CompletionResult.FromHint(Loc.GetString("command-mailto-completion-priority"));
            case 5:
                return CompletionResult.FromHint(Loc.GetString("command-mailto-completion-large"));
            default:
                return CompletionResult.Empty;
        }
    }
    // End Frontier
}

[AdminCommand(AdminFlags.Fun)]
public sealed class MailNowCommand : IConsoleCommand
{
    public string Command => "mailnow";
    public string Description => Loc.GetString("command-mailnow");
    public string Help => Loc.GetString("command-mailnow-help", ("command", Command));

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!; // Frontier

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sectorService = _entitySystemManager.GetEntitySystem<SectorServiceSystem>();
        // Frontier: sector-wide mail
        if (_entityManager.TryGetComponent(sectorService.GetServiceEntity(), out SectorMailComponent? mail))
            mail.Accumulator = (float)mail.TeleportInterval.TotalSeconds;
        // End Frontier: sector-wide mail

        shell.WriteLine(Loc.GetString("command-mailnow-success"));
    }
}
