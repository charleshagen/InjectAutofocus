using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Utility;
using NINA.Core.Utility;
using CharlesHagen.NINA.InjectAutofocus;

namespace CharlesHagen.NINA.InjectAutofocus.InjectAutofocusTestCategory; 

[ExportMetadata("Name", "Inject Autofocus")]
[ExportMetadata("Description", "This trigger will run Autofocus after the inject autofocus button is pressed at the next available opportunity")]
[ExportMetadata("Icon", "Inject_Autofocus_SVG")]
[ExportMetadata("Category", "Inject Autofocus")]
[Export(typeof(ISequenceTrigger))]
[JsonObject(MemberSerialization.OptIn)]
public class InjectAutofocusTrigger : SequenceTrigger {
    private IProfileService profileService;

    private IImageHistoryVM history;
    private ICameraMediator cameraMediator;
    private IFilterWheelMediator filterWheelMediator;
    private IFocuserMediator focuserMediator;
    private IAutoFocusVMFactory autoFocusVMFactory;

    [ImportingConstructor]
    public InjectAutofocusTrigger(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IAutoFocusVMFactory autoFocusVMFactory) : base() {
        this.history = history;
        this.profileService = profileService;
        this.cameraMediator = cameraMediator;
        this.filterWheelMediator = filterWheelMediator;
        this.focuserMediator = focuserMediator;
        this.autoFocusVMFactory = autoFocusVMFactory;

        TriggerRunner.Add(new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory));
    }

    private InjectAutofocusTrigger(InjectAutofocusTrigger cloneMe) : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory) {
        CopyMetaData(cloneMe);
    }

    public override object Clone() {
        return new InjectAutofocusTrigger(this) {
            TriggerRunner = (SequentialContainer)TriggerRunner.Clone()
        };
    }

    public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
        await TriggerRunner.Run(progress, token);
    }

    public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
        var queuedFocus = TriggerState.GetTriggerState() > 0;
        if (!queuedFocus) return false;

        if (nextItem is not IExposureItem { ImageType: not "Light" }) return false;

        // If an autofocus has occured since the last request, do not focus again, reset flag
        if (history.AutoFocusPoints.LastOrDefault() is { } lastAF &&
            lastAF.AutoFocusPoint.Time > TriggerState.GetLastRequested()) { 
            queuedFocus = false; 
        }

        if (ItemUtility.IsTooCloseToMeridianFlip(Parent, TriggerRunner.GetItemsSnapshot().First().GetEstimatedDuration() + nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero)) {
            Logger.Warning("Autofocus should be triggered, however the meridian flip is too close to be executed");
            // TODO: Check settings to see if meridian flip has AF enabled (do we want to clear the flag?)
            return false;
        }

        TriggerState.Reset();
        return queuedFocus;
    }

    public override string ToString() {
        return $"Category: {Category}, Item: {nameof(InjectAutofocusTrigger)}";
    }
}