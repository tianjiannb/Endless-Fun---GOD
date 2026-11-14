using System;
using System.Collections.Generic;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>一句台词。</summary>
    [Serializable]
    public class DialogueLine
    {
        /// <summary>说话人：chie / naron / sys / shadow / fx / move。</summary>
        public string who;
        /// <summary>台词正文。</summary>
        public string t;
    }

    /// <summary>一个选项。</summary>
    [Serializable]
    public class DialogueOption
    {
        /// <summary>选项 id，结局判定用。</summary>
        public string id;
        /// <summary>选项文字。</summary>
        public string text;
        /// <summary>选中后的回应台词。</summary>
        public DialogueLine[] reply;
    }

    /// <summary>一个对话节点。</summary>
    [Serializable]
    public class DialogueNode
    {
        /// <summary>节点 id。</summary>
        public string id;
        /// <summary>kind：lines / choice / ending。</summary>
        public string kind;
        /// <summary>到达该节点时的触发标签。</summary>
        public string tag;
        /// <summary>lines 节点正文。</summary>
        public DialogueLine[] lines;
        /// <summary>choice 节点选项。</summary>
        public DialogueOption[] options;
    }

    /// <summary>一处缺陷的两段台词。</summary>
    [Serializable]
    public class DialogueFlaw
    {
        /// <summary>缺陷键：f1..f6。</summary>
        public string id;
        /// <summary>第一次见到该缺陷时的台词。</summary>
        public DialogueLine[] intro;
        /// <summary>修好之后的台词。</summary>
        public DialogueLine[] after;
    }

    /// <summary>整份对话数据，来自 Resources/Chie/chie_dialogue.json。</summary>
    [Serializable]
    public class DialogueData
    {
        /// <summary>画作 id，回传画廊用。</summary>
        public string paintingId;
        /// <summary>开场台词。</summary>
        public DialogueLine[] opening;
        /// <summary>全部节点。</summary>
        public DialogueNode[] nodes;
        /// <summary>六处缺陷台词。</summary>
        public DialogueFlaw[] flaws;
        /// <summary>六处全修好时的站起台词。</summary>
        public DialogueLine[] standAll;
        /// <summary>未修满就放下笔时的站起台词。</summary>
        public DialogueLine[] standPart;
        /// <summary>结局前的收尾台词。</summary>
        public DialogueLine[] finale;

        private Dictionary<string, DialogueNode> _nodeMap;
        private Dictionary<string, DialogueFlaw> _flawMap;

        /// <summary>从 Resources 读取并建索引。</summary>
        public static DialogueData Load(string resourcePath)
        {
            TextAsset ta = Resources.Load<TextAsset>(resourcePath);
            if (ta == null)
            {
                Debug.LogError("[Chie] 找不到对话文件 Resources/" + resourcePath + ".json");
                return new DialogueData
                {
                    paintingId = "chie",
                    opening = new DialogueLine[0],
                    nodes = new DialogueNode[0],
                    flaws = new DialogueFlaw[0],
                    standAll = new DialogueLine[0],
                    standPart = new DialogueLine[0],
                    finale = new DialogueLine[0]
                };
            }

            DialogueData d = JsonUtility.FromJson<DialogueData>(ta.text);
            if (d == null)
            {
                Debug.LogError("[Chie] 对话文件解析失败：" + resourcePath);
                d = new DialogueData();
            }
            d.BuildIndex();
            return d;
        }

        /// <summary>建节点与缺陷索引。</summary>
        public void BuildIndex()
        {
            _nodeMap = new Dictionary<string, DialogueNode>(16);
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i] != null && !string.IsNullOrEmpty(nodes[i].id)) _nodeMap[nodes[i].id] = nodes[i];
                }
            }
            _flawMap = new Dictionary<string, DialogueFlaw>(8);
            if (flaws != null)
            {
                for (int i = 0; i < flaws.Length; i++)
                {
                    if (flaws[i] != null && !string.IsNullOrEmpty(flaws[i].id)) _flawMap[flaws[i].id] = flaws[i];
                }
            }
        }

        /// <summary>取节点，没有返回 null。</summary>
        public DialogueNode GetNode(string id)
        {
            if (_nodeMap == null) BuildIndex();
            DialogueNode n;
            return _nodeMap.TryGetValue(id, out n) ? n : null;
        }

        /// <summary>取某处缺陷台词，没有返回 null。</summary>
        public DialogueFlaw GetFlaw(FlawId f)
        {
            if (_flawMap == null) BuildIndex();
            string key = "f" + ((int)f + 1);
            DialogueFlaw d;
            return _flawMap.TryGetValue(key, out d) ? d : null;
        }
    }
}
