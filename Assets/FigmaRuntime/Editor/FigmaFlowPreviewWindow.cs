#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Editor
{
    /// <summary>
    /// v0.5/3 (ADR-019): a Preview lejátszás ablaka.
    ///
    /// ROBUSZTUSSÁG (2026-08-02 hotfix #2): az Open MINDIG újraépíti a
    /// UI-t (ForceRebuildUI) és AZONNAL hívja a Reload-ot (nem delayCall)
    /// — a régi ablak-példányok és a CreateGUI időzítés nem okozhat
    /// üres ablakot. A Load a saját schedule-ával vár a layoutra.
    /// </summary>
    public class FigmaFlowPreviewWindow : EditorWindow
    {
        private TransitionPreview preview;

        private VisualElement contentRoot;

        private VisualTreeAsset pendingFrom;
        private VisualTreeAsset pendingTo;
        private TransitionAsset pendingAsset;
        private FigmaFlowProfile pendingProfile;

        private VisualTreeAsset[] pendingStates;

        private FigmaStateMachine pendingMachine;

        private bool pendingSequence;

        private Button playButton;
        private Slider timeSlider;
        private Label infoLabel;

        private bool sliderDragging;

        // ------------------------------------------------------------

        public static void Open(
            TransitionPreview preview,
            VisualTreeAsset from,
            VisualTreeAsset to,
            TransitionAsset asset,
            FigmaFlowProfile profile)
        {
            FigmaFlowPreviewWindow win =
                GetWindow<FigmaFlowPreviewWindow>("FigmaFlow Preview");

            win.preview = preview;

            win.pendingFrom = from;
            win.pendingTo = to;
            win.pendingAsset = asset;
            win.pendingProfile = profile;

            // MINDIG teljes újraépítés + azonnali Reload.
            win.ForceRebuildUI();

            win.Reload();
        }

        /// <summary>
        /// v0.5/4: a states-alapú szekvencia megnyitása (0->1->2->...).
        /// </summary>
        public static void OpenSequence(
            TransitionPreview preview,
            VisualTreeAsset[] states,
            TransitionAsset asset,
            FigmaFlowProfile profile,
            FigmaStateMachine machine = null)
        {
            FigmaFlowPreviewWindow win =
                GetWindow<FigmaFlowPreviewWindow>("FigmaFlow Preview");

            win.preview = preview;

            win.pendingStates = states;
            win.pendingAsset = asset;
            win.pendingProfile = profile;
            win.pendingMachine = machine;

            win.pendingSequence = true;

            win.ForceRebuildUI();

            win.Reload();
        }

        // ------------------------------------------------------------

        void CreateGUI()
        {
            ForceRebuildUI();
        }

        void ForceRebuildUI()
        {
            rootVisualElement.Clear();

            // Tartalom (a Load ezt tölti/törli) — a vezérlőktől külön.
            contentRoot = new VisualElement();

            contentRoot.style.flexGrow = 1;

            rootVisualElement.Add(contentRoot);

            if (preview != null)
                preview.Initialize(contentRoot);

            // Vezérlősáv.
            VisualElement bar = new VisualElement();

            bar.style.flexDirection = FlexDirection.Row;
            bar.style.paddingLeft = 4;
            bar.style.paddingRight = 4;
            bar.style.paddingTop = 4;
            bar.style.paddingBottom = 4;
            bar.style.alignItems = Align.Center;

            playButton = new Button(OnPlayClick) { text = "Play" };

            playButton.style.width = 60;

            bar.Add(playButton);

            timeSlider = new Slider(0f, 1f);

            timeSlider.style.flexGrow = 1;

            // A húzás a Seek-et hívja; az Update nem írja felül,
            // amíg a felhasználó húzza (sliderDragging).
            timeSlider.RegisterValueChangedCallback(
                evt =>
                {
                    if (preview != null)
                        preview.Seek(evt.newValue);
                });

            timeSlider.RegisterCallback<PointerDownEvent>(
                _ => sliderDragging = true);

            timeSlider.RegisterCallback<PointerUpEvent>(
                _ => sliderDragging = false);

            bar.Add(timeSlider);

            infoLabel = new Label();

            infoLabel.style.paddingLeft = 4;
            infoLabel.style.paddingRight = 4;
            infoLabel.style.paddingTop = 4;
            infoLabel.style.paddingBottom = 4;

            rootVisualElement.Add(bar);
            rootVisualElement.Add(infoLabel);

            Debug.Log("[FigmaFlow] PreviewWindow: UI felépítve.");
        }

        void Reload()
        {
            if (preview == null)
            {
                Debug.LogWarning("[FigmaFlow] PreviewWindow.Reload: preview NULL.");

                return;
            }

            // v0.5/4: szekvencia-mód — a states tömbből.
            if (pendingSequence)
            {
                if (pendingStates == null || pendingAsset == null)
                {
                    Debug.LogWarning(
                        "[FigmaFlow] PreviewWindow.Reload: hiányzó szekvencia-bemenet.");

                    return;
                }

                preview.LoadSequence(
                    pendingStates,
                    pendingAsset,
                    pendingProfile,
                    pendingMachine);

                return;
            }

            if (pendingFrom == null ||
                pendingTo == null ||
                pendingAsset == null)
            {
                Debug.LogWarning(
                    $"[FigmaFlow] PreviewWindow.Reload: hiányzó bemenet " +
                    $"(from={pendingFrom != null}, to={pendingTo != null}, " +
                    $"asset={pendingAsset != null}).");

                return;
            }

            preview.Load(
                pendingFrom,
                pendingTo,
                pendingAsset,
                pendingProfile);
        }

        // ------------------------------------------------------------

        void OnPlayClick()
        {
            if (preview == null)
                return;

            if (preview.IsPlaying)
                preview.Stop();
            else
                preview.Play();

            playButton.text = preview.IsPlaying ? "Stop" : "Play";
        }

        void Update()
        {
            if (preview == null)
                return;

            // A fit MINDIG fut (a currentRoot létezésétől függ) —
            // nem csak IsReady-nél.
            preview.Update();

            if (!preview.IsReady)
                return;

            preview.OnEditorUpdate();

            if (playButton != null)
                playButton.text = preview.IsPlaying ? "Stop" : "Play";

            if (timeSlider != null)
            {
                timeSlider.highValue = preview.Duration;

                // Csak akkor írjuk felül az értéket, ha nem húzza
                // épp a felhasználó.
                if (!sliderDragging)
                    timeSlider.SetValueWithoutNotify(preview.Time);
            }

            if (infoLabel != null)
            {
                infoLabel.text =
                    $"{preview.Time:F2}s / {preview.Duration:F2}s · " +
                    $"{preview.TrackCount} track" +
                    (preview.SequenceMode ? preview.SequenceInfo : "");
            }
        }
    }
}

#endif
