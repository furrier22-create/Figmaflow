#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FigmaRuntime.Editor
{
    /// <summary>
    /// v0.5/5 (ADR-021): a TransitionAsset vizuális idővonala.
    ///
    /// UI Toolkit-alapú ablak (a Motion Studio IMGUI-s — ezért külön
    /// ablak, mint a Preview). Minden item egy sor:
    /// - path név
    /// - sáv a delay -> delay+duration intervallummal
    /// - a sáv húzható: eleje (delay), vége (duration), közepe (eltolás)
    /// - az ease legördülő (a sáv mellett)
    ///
    /// A lejátszási vonal (playhead) a Preview Time-ját mutatja; a
    /// sávra kattintás a Preview-t odaugrasztja (seek); az üres sávra
    /// kattintás az adott időpontra ugraszt.
    ///
    /// JAVÍTÁS (2026-08-02): a px<->mp konverzió helyesen
    /// pxPerSec = laneW / total (korábban felcserélve → minden sáv a
    /// bal szélre, 4px-re zsugorodott = "egy kék vonal"). A húzás
    /// ablak-koordinátából, lane-hez viszonyítva (a localPosition a
    /// sávhoz relatív lenne — a húzás közben eltolódna). Pointer-capture
    /// a robusztus húzáshoz.
    ///
    /// A módosítások az assetbe mentődnek (SetDirty).
    /// </summary>
    public class FigmaFlowTimelineWindow : EditorWindow
    {
        private TransitionPreview preview;

        private TransitionAsset asset;

        private VisualElement tracksRoot;

        private VisualElement playhead;

        private System.Collections.Generic.Dictionary<TransitionItem, VisualElement> bars =
            new();

        private float trackHeight = 36f;

        private float leftPadding = 160f;

        private float rightPadding = 20f;

        // ------------------------------------------------------------

        public static void Open(
            TransitionPreview preview,
            TransitionAsset asset)
        {
            FigmaFlowTimelineWindow win =
                GetWindow<FigmaFlowTimelineWindow>("FigmaFlow Timeline");

            win.preview = preview;
            win.asset = asset;

            win.ForceRebuildUI();
        }

        // ------------------------------------------------------------

        void CreateGUI()
        {
            ForceRebuildUI();
        }

        void ForceRebuildUI()
        {
            rootVisualElement.Clear();

            bars.Clear();

            Label header = new Label(
                asset != null ? asset.name : "Timeline");

            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.paddingLeft = 8;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;

            rootVisualElement.Add(header);

            ScrollView scroll = new ScrollView();

            scroll.style.flexGrow = 1;

            tracksRoot = new VisualElement();

            tracksRoot.style.flexGrow = 1;

            scroll.Add(tracksRoot);

            rootVisualElement.Add(scroll);

            playhead = new VisualElement();

            playhead.style.position = Position.Absolute;
            playhead.style.width = 2;
            playhead.style.backgroundColor = new Color(1f, 0.3f, 0.2f, 0.9f);
            playhead.style.top = 28;
            playhead.style.bottom = 0;
            playhead.pickingMode = PickingMode.Ignore;

            rootVisualElement.Add(playhead);

            RebuildTracks();
        }

        void RebuildTracks()
        {
            if (tracksRoot == null)
                return;

            bars.Clear();

            tracksRoot.Clear();

            if (asset == null || asset.items == null)
            {
                tracksRoot.Add(
                    new Label("Nincs asset betöltve."));

                return;
            }

            foreach (TransitionItem item in asset.items)
            {
                tracksRoot.Add(CreateTrackRow(item));
            }
        }

        VisualElement CreateTrackRow(TransitionItem item)
        {
            VisualElement row = new VisualElement();

            row.style.height = trackHeight;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            Label label = new Label(item.path);

            label.style.width = leftPadding - 8;
            label.style.paddingLeft = 8;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 11;

            row.Add(label);

            VisualElement lane = new VisualElement();

            lane.style.flexGrow = 1;
            lane.style.height = trackHeight;
            lane.style.position = Position.Relative;

            VisualElement bar = new VisualElement();

            bar.style.position = Position.Absolute;
            bar.style.top = 8;
            bar.style.height = 20;
            bar.style.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.8f);
            bar.style.borderTopLeftRadius = 4;
            bar.style.borderBottomLeftRadius = 4;
            bar.style.borderTopRightRadius = 4;
            bar.style.borderBottomRightRadius = 4;

            bars[item] = bar;

            // Kattintás a sávra → seek a sáv elejére + capture.
            bar.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (preview != null)
                    preview.Seek(item.delay);

                bar.CapturePointer(evt.pointerId);

                evt.StopPropagation();
            });

            // Húzás: a lane-hez viszonyított ablak-koordinátából.
            bar.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!bar.HasPointerCapture(evt.pointerId))
                    return;

                VisualElement laneParent = bar.parent;

                if (laneParent == null)
                    return;

                float laneW = laneParent.worldBound.width;
                float total = GetTotalDuration();

                if (laneW <= 0f || total <= 0f)
                    return;

                float pxPerSec = laneW / total;

                // A pozíció a lane-hez viszonyítva (nem a sávhoz!).
                float x = evt.position.x - laneParent.worldBound.x;

                float seconds = x / pxPerSec;

                float barX = item.delay * pxPerSec;
                float barW = Mathf.Max(4f, (item.delay + item.duration) * pxPerSec);

                float rel = (x - barX) / barW;

                if (rel < 0.15f)
                {
                    // delay húzás.
                    float newDelay = Mathf.Max(0f, seconds);

                    float delta = newDelay - item.delay;

                    item.delay = newDelay;
                    item.duration = Mathf.Max(0.05f, item.duration - delta);
                }
                else if (rel > 0.85f)
                {
                    // duration húzás.
                    item.duration =
                        Mathf.Max(0.05f, seconds - item.delay);
                }
                else
                {
                    // eltolás (a sáv közepe a pozícióra).
                    float half = (item.delay + item.duration) * 0.5f;

                    item.delay = Mathf.Max(0f, seconds - half);
                }

                EditorUtility.SetDirty(asset);
            });

            bar.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (bar.HasPointerCapture(evt.pointerId))
                    bar.ReleasePointer(evt.pointerId);
            });

            lane.Add(bar);

            // Kattintás az üres sávra → seek az adott időpontra.
            lane.RegisterCallback<PointerDownEvent>(evt =>
            {
                float x = evt.position.x - lane.worldBound.x;

                float laneW = lane.worldBound.width;
                float total = GetTotalDuration();

                if (laneW > 0f && total > 0f && preview != null)
                {
                    preview.Seek(x / (laneW / total));
                }
            });

            EnumField easeField =
                new EnumField(item.ease);

            easeField.style.width = 130;

            easeField.RegisterValueChangedCallback(evt =>
            {
                item.ease = (EaseType)evt.newValue;

                EditorUtility.SetDirty(asset);
            });

            row.Add(lane);
            row.Add(easeField);

            return row;
        }

        float GetTotalDuration()
        {
            if (asset == null || asset.items == null)
                return 0f;

            float total = 0f;

            foreach (TransitionItem item in asset.items)
            {
                total = Mathf.Max(
                    total,
                    item.delay + item.duration);
            }

            return Mathf.Max(total, 0.1f);
        }

        void LayoutTrack(TransitionItem item)
        {
            if (!bars.TryGetValue(item, out VisualElement bar))
                return;

            VisualElement lane = bar.parent;

            if (lane == null)
                return;

            float laneW = lane.worldBound.width;

            float total = GetTotalDuration();

            if (laneW <= 0f || total <= 0f)
                return;

            // pxPerSec: hány képpont = 1 másodperc a sávon.
            float pxPerSec = laneW / total;

            bar.style.left = item.delay * pxPerSec;

            bar.style.width = Mathf.Max(4f, item.duration * pxPerSec);
        }

        // ------------------------------------------------------------

        void Update()
        {
            if (asset != null && tracksRoot != null)
            {
                foreach (TransitionItem item in asset.items)
                {
                    if (bars.ContainsKey(item))
                        LayoutTrack(item);
                }
            }

            if (playhead != null && preview != null && preview.IsReady)
            {
                float total = GetTotalDuration();

                float laneW =
                    tracksRoot != null ? tracksRoot.worldBound.width : 0f;

                if (laneW > 0f && total > 0f)
                {
                    float x =
                        leftPadding +
                        (preview.Time / total) * (laneW - leftPadding - rightPadding);

                    playhead.style.left = x;
                }
            }
        }
    }
}

#endif
