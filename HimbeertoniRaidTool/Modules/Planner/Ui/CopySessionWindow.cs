using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Planner.Ui;

public class CopySessionWindow : HrtWindow
{

    private DateTime _targetStart;
    private readonly RaidSession _session;
    public CopySessionWindow(IUiSystem uiSystem, RaidSession session) : base(uiSystem)
    {
        _session = session;
        Title = $"Make copy of {session.Title} ";
        Size = new Vector2(250, 170);
        OpenCentered = true;
        _targetStart = session.StartTime;
    }

    public override void Draw()
    {
        if (ImGuiHelper.Button(FontAwesomeIcon.Copy, "Copy", "Copy the current session to the specified time"))
            Save();
        InputHelper.InputDateTime("targetTime", ref _targetStart, "New Start Time");
    }

    private void Save()
    {
        var newSession = new RaidSession();
        newSession.CopyFrom(_session);
        newSession.StartTime = _targetStart;
        foreach (var participant in newSession.Participants)
        {
            participant.InvitationStatus = InviteStatus.Invited;
            participant.ParticipationStatus = ParticipationStatus.NoStatus;
        }
        foreach (var instanceSession in newSession.PlannedContent)
        {
            instanceSession.Killed = false;
            instanceSession.Loot.Clear();
        }
        UiSystem.GetHrtDataManager().RaidSessionDb.TryAdd(newSession);
        Hide();
    }
}