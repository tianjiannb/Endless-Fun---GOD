using System;
using UnityEngine;

namespace SplitSlide
{
    /// <summary>Filename-based mapping of the supplied transparent artwork; source PNGs stay intact.</summary>
    public sealed class PuzzleArtwork
    {
        public Sprite Floor { get; private set; }
        public Sprite Wall { get; private set; }
        public Sprite Splitter { get; private set; }
        public Sprite Goal { get; private set; }
        public Sprite SwitchOn { get; private set; }
        public Sprite SwitchOff { get; private set; }
        public Sprite DoorClosed { get; private set; }
        public Sprite DoorOpen { get; private set; }
        public Sprite Player { get; private set; }
        public Sprite Backdrop { get; private set; }

        public static PuzzleArtwork Load() => new PuzzleArtwork
        {
            Floor = CropFloor(Require("10_莲池油画地板")),
            Wall = Require("02_墙壁"),
            Splitter = Require("03_分裂格"),
            Goal = Require("04_目标格"),
            SwitchOn = Require("05_开关格_开启"),
            SwitchOff = Require("06_开关格_关闭"),
            DoorClosed = Require("07_门_关闭"),
            DoorOpen = Require("08_门_开启"),
            Player = CropPlayer(Require("09_玩家细胞")),
            Backdrop = Require("11_叶脉油画背景")
        };

        private static Sprite CropFloor(Sprite source)
        {
            // Presentation crop only: hide the photograph's white edge without modifying its pixels.
            Rect rect = source.rect;
            var cropped = Sprite.Create(source.texture,
                new Rect(rect.x + rect.width * .06f, rect.y + rect.height * .06f,
                    rect.width * .88f, rect.height * .88f), new Vector2(.5f, .5f), 100,
                0, SpriteMeshType.FullRect);
            cropped.name = source.name;
            return cropped;
        }

        private static Sprite CropPlayer(Sprite source)
        {
            // The supplied 1080x1920 PNG has large transparent margins. Display just the cell
            // plus 12 source pixels of padding; retain the original PNG and its alpha unchanged.
            Rect rect = source.rect;
            var cropped = Sprite.Create(source.texture,
                new Rect(rect.x + rect.width * (66f / 1080), rect.y + rect.height * (509f / 1920),
                    rect.width * (945f / 1080), rect.height * (900f / 1920)),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            cropped.name = source.name;
            return cropped;
        }

        public void Release()
        {
            // Cropped sprites belong to the view; imported resource sprites do not.
            if (Floor != null) UnityEngine.Object.Destroy(Floor);
            if (Player != null) UnityEngine.Object.Destroy(Player);
        }

        private static Sprite Require(string name)
        {
            Sprite sprite = Resources.Load<Sprite>("SplitSlideArt/" + name);
            if (sprite == null) throw new InvalidOperationException($"缺少玩法美术：{name}，请将素材导入为 Sprite（2D 和 UI）。");
            return sprite;
        }
    }
}
