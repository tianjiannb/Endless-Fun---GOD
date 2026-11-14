using System.Collections.Generic;
using System.IO;
using Numen.Chie;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Numen.Chie.EditorTools
{
    /// <summary>Tools/Chie/生成字体资源：从系统字体造一份动态 SDF 中文 TMP 字体。</summary>
    public static class ChieFontBuilder
    {
        private const string Dir = "Assets/Paintings/Chie/Resources/Chie/Fonts";
        private const string TtfPath = Dir + "/ChieSource.ttf";
        private const string AssetPath = Dir + "/ChieSDF.asset";
        private const string ConfigPath = "Assets/Paintings/Chie/Resources/Chie/ChieConfig.asset";

        private const int SamplingSize = 48;
        private const int Padding = 5;
        private const int AtlasSize = 1024;

        // 常用字（约 500）+ 本项目专有字；其余靠 Dynamic 模式运行时补
        private const string Common =
            "的一是不了在人有我他这个们中来上大为和国地到以说时要就出会可也你对生能而子那得于着下自之年过发后作里用道行所然家种事成方多经么去法学如都同现当没动面起看定天分还进好小部其些主样理心她本前开但因只从想实日军者意无力它与长把机十民第公此已工使情明性知全三又关点正业外将两高间由问很最重并物手应战向头文体政美相见被利什二等产或新己制身果加西斯月话合回特代内信表化老给世位次度门任常先海通教儿原东声提立及比员解水名真论处走义各入几口认条平系气题活尔更别打女变四神总何电数安少报才结反受目太量再感建务做接必场件计管期市直德资命山金指克许统区保至队形社便空决治展马科司五基眼书非则听白却界达光放强即像难且权思王象完设式色路记南品住告类求据程北边死张该交规万取拉格望觉术领共确传师观清今切院让识候带导争运笑飞风步改收根干造言联持组每济车亲极林服快办议往元英士证近失转夫令准布始怎呢存未远叫台单影具罗字爱击流备兵连调深商算质团集百需价花党华城石级整府离况亚请技际约示复病息究线似官火断精满支视消越器容照须九增研写称企八功吗包片史委乎查轻易早曾除农找装广显吧阿李标谈吃图念六引历首医局突专费号尽另周较注语仅考落青随选列武红响虽推势参希古众构房半节土投某案黑维革划敌致陈律足态护七兴派孩验责营星够章音跟志底站严巴例防族供效续施留讲型料终答紧黄绝奇察母京段依批群项故按河米围江织害斗双境客纪采举杀攻父苏密低朝友诉止细愿千值仍男钱破网热助倒育属坐帝限船脸职速刻乐否刚威毛状率甚独球般普怕弹校苦创假久错承印晚兰试股拿脑预谁益阳若哪微尼继送急血惊伤素药适波夜省初喜卫源食险待述陆习置居劳财环排福纳欢雷警获模充负云停木游龙树疑层冷洲冲射略范竟句室异激汉村哈策演简卡罪判担州静退既衣您宗积余痛检差富灵协角占配征修皮挥胜降阶审沉坚善妈刘读超免压银买皇养伊怀执副乱抗犯追帮宣佛岁航优怪香著田铁控税左右份穿艺背阵草脚概恶块顿敢守酒岛托央户烈洋哥索胡款靠评版宝座释景顾弟登货互付伯慢欧换闻危忙核暗姐介坏讨丽良序升监临亮露永呼味野架域沙掉括舰鱼杂误湾吉减编楚肯测败屋跑梦散温困剑渐封救贵枪缺楼县尚毫移娘朋画班智亦耳恩短掌恐遗固席松秘谢鲁遇康虑幸均销钟诗藏赶剧票损忽巨炮旧端探湖录叶春乡附吸予礼港雨呀板庭妇归睛饭额含顺输摇招婚脱补谓督毒油疗旅泽材灭逐莫笔亡鲜词圣择寻厂睡博勒烟授诺伦岸奥唐卖俄炸载洛健堂旁宫喝借君禁阴园谋宋避抓荣姑孙逃牙束跳顶玉镇雪午练迫爷篇肉嘴馆遍凡础洞卷坦牛宁纸诸训私庄祖丝翻暴森塔默握戏隐熟骨访弱蒙歌店鬼软典欲伙遭盘爸扩盖弄雄稳忘亿刺拥徒姆杨齐赛趣曲刀床迎冰虚玩析窗醒妻透购替塞努休虎扬途侵刑绿兄迅套贸毕唯谷轮库迹尤竞街促延震弃甲伟麻川申轩甘";

        private static readonly string[] Candidates =
        {
            "C:/Windows/Fonts/Deng.ttf",
            "C:/Windows/Fonts/msyh.ttc",
            "C:/Windows/Fonts/simhei.ttf",
            "C:/Windows/Fonts/simsun.ttc",
        };

        /// <summary>菜单入口。</summary>
        [MenuItem("Tools/Chie/生成字体资源")]
        public static void BuildAll() { BuildAllBatch(); }

        /// <summary>批处理入口。</summary>
        public static void BuildAllBatch()
        {
            EnsureDir(Dir);

            if (!File.Exists(TtfPath))
            {
                string src = null;
                for (int i = 0; i < Candidates.Length; i++)
                    if (File.Exists(Candidates[i])) { src = Candidates[i]; break; }
                if (src == null)
                {
                    Debug.LogError("[Chie] 系统里找不到中文字体，请手动放一份 ttf 到 " + TtfPath);
                    return;
                }
                File.Copy(src, TtfPath, true);
                AssetDatabase.ImportAsset(TtfPath, ImportAssetOptions.ForceUpdate);
                Debug.Log("[Chie] 字体源已复制：" + src + " → " + TtfPath);
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null)
            {
                Debug.LogError("[Chie] 字体源导入失败：" + TtfPath);
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            if (existing != null) AssetDatabase.DeleteAsset(AssetPath);

            TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, SamplingSize, Padding,
                GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
            if (fa == null)
            {
                Debug.LogError("[Chie] 创建 TMP 字体失败，请确认字体允许 Include Font Data");
                return;
            }
            fa.name = "ChieSDF";
            AssetDatabase.CreateAsset(fa, AssetPath);
            AttachSubAssets(fa);

            // TMP 3.0.7 的 SetupNewAtlasTexture 会读 m_AtlasTexture.name；
            // 该字段只在 ReadFontAssetDefinition / atlasTexture 取值时才被填上，必须先预热。
            if (fa.atlasTexture == null)
            {
                Debug.LogError("[Chie] atlasTexture 预热失败，字体图集未初始化");
                return;
            }

            string chars = CollectChars();
            fa.TryAddCharacters(chars, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.Log("[Chie] 字体缺字（运行时按需补）：" + missing.Length + " 个");

            if (fa.atlasTextures != null)
            {
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                    if (fa.atlasTextures[i] != null) fa.atlasTextures[i].Apply(false, false);
            }
            AttachSubAssets(fa);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);

            var cfg = AssetDatabase.LoadAssetAtPath<ChieConfig>(ConfigPath);
            if (cfg != null)
            {
                var so = new SerializedObject(cfg);
                var prop = so.FindProperty("_font");
                if (prop != null) prop.objectReferenceValue = fa;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cfg);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogWarning("[Chie] 还没有 ChieConfig，先跑「生成占位资源」再回填字体");
            }
            Debug.Log("[Chie] TMP 字体已生成：" + AssetPath + "（预烘 " + chars.Length + " 字，图集 " + fa.atlasTextures.Length + " 张）");
        }

        private static void AttachSubAssets(TMP_FontAsset fa)
        {
            if (fa.atlasTextures != null)
            {
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                {
                    Texture2D t = fa.atlasTextures[i];
                    if (t == null) continue;
                    t.name = "ChieSDF Atlas " + i;
                    if (!AssetDatabase.Contains(t)) AssetDatabase.AddObjectToAsset(t, fa);
                }
            }
            if (fa.material != null)
            {
                fa.material.name = "ChieSDF Material";
                if (!AssetDatabase.Contains(fa.material)) AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
        }

        /// <summary>对话里出现的字 + 常用字 + 标点。</summary>
        private static string CollectChars()
        {
            var set = new HashSet<char>();
            for (int i = 0; i < Common.Length; i++) set.Add(Common[i]);

            const string extra =
                "画拿起笔放橡皮吸管颜色窗外嘴角五官影子笔尖窗台灰修好六处三波千绘纳伦站走到门画廊再来一起去你自己我还会" +
                "涂擦看想让改错对没还差一点点就";

            var json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Paintings/Chie/Resources/Chie/chie_dialogue.json");
            string text = json != null ? json.text : "";
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\u000A' || c == '\u000D' || c == '\t') continue;
                set.Add(c);
            }
            for (int i = 0; i < extra.Length; i++) set.Add(extra[i]);
            for (char c = '0'; c <= '9'; c++) set.Add(c);
            for (char c = 'A'; c <= 'Z'; c++) set.Add(c);
            for (char c = 'a'; c <= 'z'; c++) set.Add(c);
            const string punct = "，。！？：；、“”‘’（）《》…—·,.!?:;\"'()[]{}<>-+=/% ";
            for (int i = 0; i < punct.Length; i++) set.Add(punct[i]);

            var arr = new char[set.Count];
            set.CopyTo(arr);
            return new string(arr);
        }

        private static void EnsureDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
