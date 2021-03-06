using System;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Timeline.Data;

namespace UnityEditor.Timeline
{
    [TimelineEditor(typeof(XTransformTrack))]
    public class EditorTransformTrack : EditorTrack
    {
        static GUIContent s_RecordOn;
        static GUIContent s_RecordOff;
        static GUIContent s_KeyOn;
        static GUIContent s_KeyOff;
        private TransformTrackData Data;

        protected override Color trackColor
        {
            get { return new Color(0f, 1.0f, 0.8f); }
        }

        protected override bool warn
        {
            get { return track.parent == null || Data.time == null; }
        }

        protected override string trackHeader
        {
            get { return "位移" + ID; }
        }

        protected override void OnAddClip(float time)
        {
            throw new Exception("transform no clips");
        }

        private void InitStyle()
        {
            if (s_RecordOn == null)
            {
                s_RecordOn = new GUIContent(TimelineStyles.autoKey.active.background);
            }
            if (s_RecordOff == null)
            {
                s_RecordOff = new GUIContent(TimelineStyles.autoKey.normal.background);
            }
            if (s_KeyOn == null)
            {
                s_KeyOn = new GUIContent(TimelineStyles.keyframe.active.background);
            }
            if (s_KeyOff == null)
            {
                s_KeyOff = new GUIContent(TimelineStyles.keyframe.normal.background);
            }
        }

        protected override void OnGUIHeader()
        {
            InitStyle();
            bool recd = track.record;
            var content = recd ? s_RecordOn : s_RecordOff;

            if (recd)
            {
                float remainder = Time.realtimeSinceStartup % 1;
                TimelineWindow.inst.Repaint();
                if (remainder < 0.3f)
                {
                    content = TimelineStyles.empty;
                    addtiveColor = Color.white;
                }
                else
                {
                    addtiveColor = Color.red;
                }
            }
            else
            {
                addtiveColor = Color.white;
            }

            if (GUILayout.Button(content, TimelineStyles.autoKey, GUILayout.MaxWidth(16)))
            {
                if (recd)
                {
                    StopRecd();
                }
                else
                {
                    StartRecd();
                }
                track.SetFlag(TrackMode.Record, !recd);
            }
            if (go && !track.locked)
            {
                ProcessTansfEvent();
            }
        }


        protected override void OnGUIContent()
        {
            if (Data == null)
            {
                var tt = (track as XTransformTrack);
                Data = tt?.Data;
            }
            if (Data?.time != null)
            {
                for (int i = 0; i < Data.time.Length; i++)
                {
                    Rect r = RenderRect;
                    r.x = TimelineWindow.inst.TimeToPixel(Data.time[i]);
                    if (TimelineWindow.inst.IsPiexlRange(r.x))
                    {
                        r.width = 20;
                        r.y = RenderRect.y + RenderRect.height / 3;
                        GUIContent gct = Data.@select ? s_KeyOn : s_KeyOff;
                        GUI.Box(r, gct, TimelineStyles.keyframe);
                    }
                }
            }
        }


        private void ProcessTansfEvent()
        {
            var e = Event.current;
            if (recoding)
            {
                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.F)
                    {
                        PrepareOperation(e.mousePosition);
                    }
                    if(e.keyCode == KeyCode.D || e.keyCode== KeyCode.Delete)
                    {
                        DeleteItem(e.mousePosition);
                    }
                }
                if (e.type == EventType.MouseDown)
                {
                    var t = TimelineWindow.inst.PiexlToTime(e.mousePosition.x);
                    if (ContainsT(t, out var i))
                    {
                        Data.@select = !Data.@select;
                        e.Use();
                    }
                }
            }
        }

        protected override void OnSelect()
        {
            base.OnSelect();
            if (track.root != null)
            {
                var t = track.root as XBindTrack;
                if (t?.bindObj != null)
                {
                    Selection.activeGameObject = t.bindObj;
                }
            }
        }

        protected override void OnInspectorTrack()
        {
            EditorGUILayout.LabelField("recoding: " + recoding);
            if (track.parent == null)
                EditorGUILayout.HelpBox("no parent bind", MessageType.Warning);
            if (Data?.time != null)
            {
                if (go) EditorGUILayout.LabelField("target: " + go.name);
                for (int i = 0; i < Data.time.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("time: " + Data.time[i]);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("x", GUI.skin.label, GUILayout.MaxWidth(20)))
                    {
                        (track as XTransformTrack).RmItemAt(i);
                        TimelineWindow.inst.Repaint();
                        EditorGUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                    Vector3 pos = Data.pos[i];
                    pos = EditorGUILayout.Vector3Field("pos", pos);
                    float rot = Data.pos[i].w;
                    rot = EditorGUILayout.FloatField("rotY", rot);
                    Data.pos[i] = new Vector4(pos.x, pos.y, pos.z, rot);
                    EditorGUILayout.Space();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("not config time frame", MessageType.Warning);
            }
        }

        private GameObject go;
        private bool recoding;


        private void PrepareOperation(Vector2 pos)
        {
            float t = TimelineWindow.inst.PiexlToTime(pos.x);
            if (ContainsT(t, out var i))
            {
                if (EditorUtility.DisplayDialog("tip", "Do you want delete item", "ok", "no"))
                {
                    RmItem(i);
                }
                GUIUtility.ExitGUI();
            }
            else
            {
                AddItem(t);
            }
        }

        private void DeleteItem(Vector2 pos)
        {
            float t = TimelineWindow.inst.PiexlToTime(pos.x);
            if (ContainsT(t, out var i, 0.4f))
            {
                RmItem(i);
            }
        }

        private bool ContainsT(float t,  out int i, float max=0.1f)
        {
            i = 0;
            var time = Data.time;
            if (time != null)
            {
                for (int j = 0; j < time.Length; j++)
                {
                    if (Mathf.Abs(time[j] - t) < max)
                    {
                        i = j;
                        return true;
                    }
                }
            }
            return false;
        }

        private void AddItem(float t)
        {
            var tt = track as XTransformTrack;
            tt.AddItem(t, go.transform.localPosition, go.transform.localEulerAngles);
            TimelineWindow.inst.Repaint();
        }

        private void RmItem(int i)
        {
            var tt = track as XTransformTrack;
            if (tt.RmItemAt(i)) TimelineWindow.inst.Repaint();
        }

        private void StartRecd()
        {
            if (track.parent)
            {
                if (track.parent is XBindTrack bind && bind.bindObj != null)
                {
                    go = bind.bindObj;
                    recoding = true;
                    TimelineWindow.inst.tree?.SetRecordTrack(this);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("warn", "parent track is null or not bind", "ok");
            }
        }

        private void StopRecd()
        {
            recoding = false;
        }
    }
}
