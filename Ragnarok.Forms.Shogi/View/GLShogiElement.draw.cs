﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

using SharpGL;
using Ragnarok;
using Ragnarok.Shogi;
using Ragnarok.Utility;
using Ragnarok.ObjectModel;
using Ragnarok.Extra.Effect;

namespace Ragnarok.Forms.Shogi.View
{
    using Effect;

    /// <summary>
    /// 盤面の描画を行います。
    /// </summary>
    public partial class GLShogiElement
    {
        private readonly RectangleF[] pieceBoxBounds = new RectangleF[3];
        private Dictionary<BoardPiece, Mesh> pieceMeshDic;
        private GL.Texture boardTexture;
        private GL.Texture pieceTexture;
        private GL.Texture pieceBoxTexture;

        private readonly GL.TextTexture[] nameTexture = new GL.TextTexture[3];
        private readonly GL.TextTexture[] timeTexture = new GL.TextTexture[3];

        #region 初期化
        /// <summary>
        /// 描画関係の初期化を行います。
        /// </summary>
        private void InitializeDraw()
        {
            InitializeBounds();
            InitializePieceMesh();

            // ハンドラの設定を行います。
            AddPropertyChangedHandler("BoardBitmap", BoardBitmapUpdated);
            AddPropertyChangedHandler("PieceBitmap", PieceBitmapUpdated);
            AddPropertyChangedHandler("PieceBoxBitmap", PieceBoxBitmapUpdated);

            // デフォルトのプロパティ値も設定します。
            LocalTransform.Scale(1.0 / 640.0, 1.0 / 360.0, 1.0);
            IsTimeVisible = true;
            TebanPlayerNameBackgroundColor = Color.FromArgb(128, Color.Orange);
            UnTebanPlayerNameBackgroundColor = Color.FromArgb(128, Color.Black);
            TimeBackgroundColor = Color.FromArgb(128, Color.Black);
            BoardOpacity = 1.0;
        }

        /// <summary>
        /// OpenGL初期化後の描画関係の初期化を行います。
        /// </summary>
        public override void OnOpenGLInitialized(EventArgs e)
        {
            base.OnOpenGLInitialized(e);
            var gl = OpenGL;
            
            for (var i = 0; i < 3; ++i)
            {
                this.nameTexture[i] = new GL.TextTexture(gl);
                this.timeTexture[i] = new GL.TextTexture(gl);
            }

            this.boardTexture = new GL.Texture(gl);
            this.pieceTexture = new GL.Texture(gl);
            this.pieceBoxTexture = new GL.Texture(gl);

            // イベントハンドラの設定後にテクスチャの実体化などの、設定を行います。
            BoardBitmap = DefaultBoardBitmap;
            PieceBoxBitmap = DefaultPieceBoxBitmap;
            PieceBitmap = DefaultPieceBitmap;
        }

        /// <summary>
        /// 盤などのサイズを設定します。
        /// </summary>
        private void InitializeBounds()
        {
            var w2 = 340 / 2;
            var h2 = 350 / 2;
            BoardBounds = new RectangleF(
                320 - w2, 180 - h2, w2 * 2, h2 * 2);

            // 駒の表示サイズを設定
            SquareSize = new SizeF(
                (float)(BoardBounds.Width / (Board.BoardSize + BoardBorderRate * 2)),
                (float)(BoardBounds.Height / (Board.BoardSize + BoardBorderRate * 2)));

            // 盤サイズの設定
            BoardSquareBounds = new RectangleF(
                (float)(BoardBounds.X + SquareSize.Width * BoardBorderRate),
                (float)(BoardBounds.Y + SquareSize.Height * BoardBorderRate),
                SquareSize.Width * Board.BoardSize,
                SquareSize.Height * Board.BoardSize);

            // 駒台のサイズ / 2
            w2 = 120 / 2;
            h2 = 170 / 2;

            // index=0が駒箱の駒となります。
            this.pieceBoxBounds[0] = new RectangleF(
                320 - 235 - w2, 360 - 5 - h2 * 2, w2 * 2, h2 * 2);
            this.pieceBoxBounds[1] = new RectangleF(
                320 + 235 - w2, 360 - 5 - h2 * 2, w2 * 2, h2 * 2);
            this.pieceBoxBounds[2] = new RectangleF(
                320 - 235 - w2, 5, w2 * 2, h2 * 2);
        }

        /// <summary>
        /// 各駒の描画に使うメッシュを初期化します。
        /// </summary>
        private void InitializePieceMesh()
        {
            var pieceList =
                from bwType in EnumEx.GetValues<BWType>()
                from pieceType in EnumEx.GetValues<PieceType>()
                from promoted in new[] { false, true }
                let piece = new BoardPiece(pieceType, promoted, bwType)
                where piece.Validate()
                select piece;

            this.pieceMeshDic = pieceList
                .ToDictionary(_ => _, _ => CreatePieceMesh(_));
        }

        /// <summary>
        /// 駒の描画に使うメッシュを作成します。
        /// </summary>
        private Mesh CreatePieceMesh(BoardPiece piece)
        {
            var uv = GetPieceTextureUV(piece);

            return new Mesh(
                // 頂点配列
                new Point3d[]
                {
                    new Point3d(-0.5, -0.5, 0.0),
                    new Point3d(+0.5, -0.5, 0.0),
                    new Point3d(-0.5, +0.5, 0.0),
                    new Point3d(+0.5, +0.5, 0.0),
                },
                // テクスチャUV配列
                new Pointd[]
                {
                    new Pointd(uv.Left, uv.Top),
                    new Pointd(uv.Right, uv.Top),
                    new Pointd(uv.Left, uv.Bottom),
                    new Pointd(uv.Right, uv.Bottom),
                },
                // インデックス配列
                new int[]
                {
                    0, 2, 1,
                    1, 2, 3,
                });
        }

        /// <summary>
        /// 駒画像のUVを取得します。
        /// </summary>
        private RectangleF GetPieceTextureUV(BoardPiece piece)
        {
            var tyi = ImageIndexY(piece) + (!piece.IsPromoted ? 0 : 1);
            var txi = (int)piece.PieceType - 1;

            return new RectangleF(txi / 8.0f, tyi / 4.0f, 1.0f / 8.0f, 1.0f / 4.0f);
        }

        /// <summary>
        /// 駒テクスチャのＹ方向インデックスを取得します。
        /// </summary>
        private int ImageIndexY(BoardPiece piece)
        {
            if (piece.BWType == BWType.None)
            {
                // 駒箱の駒は常に０
                return 0;
            }
            else
            {
                // 駒台の駒は視点と同じなら０
                return (piece.BWType == ViewSide ? 0 : 2);
            }
        }
        #endregion

        #region 見た目のプロパティ
        /// <summary>
        /// 時間の表示を行うかどうかを取得または設定します。
        /// </summary>
        public bool IsTimeVisible
        {
            get { return GetValue<bool>("IsTimeVisible"); }
            set { SetValue("IsTimeVisible", value); }
        }

        /// <summary>
        /// テクスチャを読み込みます。
        /// </summary>
        private void LoadTexture(GL.Texture texture, Bitmap bitmap)
        {
            if (bitmap != null)
            {
                if (!texture.Create(bitmap))
                {
                    throw new RagnarokException(
                        "テクスチャの作成に失敗しました。");
                }
            }
            else
            {
                texture.Destroy();
            }
        }

        /// <summary>
        /// 駒画像を取得または設定します。
        /// </summary>
        public Bitmap PieceBitmap
        {
            get { return GetValue<Bitmap>("PieceBitmap"); }
            set { SetValue("PieceBitmap", value); }
        }

        /// <summary>
        /// PieceBitmapの更新時に呼ばれ、テクスチャの更新を行います。
        /// </summary>
        private void PieceBitmapUpdated(object sender, PropertyChangedEventArgs e)
        {
            LoadTexture(this.pieceTexture, PieceBitmap);
        }

        /// <summary>
        /// 盤画像を取得または設定します。
        /// </summary>
        public Bitmap BoardBitmap
        {
            get { return GetValue<Bitmap>("BoardBitmap"); }
            set { SetValue("BoardBitmap", value); }
        }

        /// <summary>
        /// 盤画像の更新時に呼ばれ、テクスチャの更新を行います。
        /// </summary>
        private void BoardBitmapUpdated(object sender, PropertyChangedEventArgs e)
        {
            LoadTexture(this.boardTexture, BoardBitmap);
        }

        /// <summary>
        /// 駒台画像を取得または設定します。
        /// </summary>
        public Bitmap PieceBoxBitmap
        {
            get { return GetValue<Bitmap>("PieceBoxBitmap"); }
            set { SetValue("PieceBoxBitmap", value); }
        }

        /// <summary>
        /// 駒台画像の更新時に呼ばれ、テクスチャの更新を行います。
        /// </summary>
        private void PieceBoxBitmapUpdated(object sender, PropertyChangedEventArgs e)
        {
            LoadTexture(this.pieceBoxTexture, PieceBoxBitmap);
        }

        /// <summary>
        /// 駒箱を表示するかどうかを取得します。
        /// </summary>
        [DependOnProperty("EditMode")]
        public bool IsKomaBoxVisible
        {
            get { return (EditMode == EditMode.Editing); }
        }

        /// <summary>
        /// 盤と駒台画像の不透明度を取得または設定します。
        /// </summary>
        public double BoardOpacity
        {
            get { return GetValue<double>("BoardOpacity"); }
            set { SetValue("BoardOpacity", value); }
        }

        /// <summary>
        /// 手番側の対局者の名前の背景色を取得または設定します。
        /// </summary>
        public Color TebanPlayerNameBackgroundColor
        {
            get { return GetValue<Color>("TebanPlayerNameBackgroundColor"); }
            set { SetValue("TebanPlayerNameBackgroundColor", value); }
        }

        /// <summary>
        /// 手番ではない方の対局者の名前の背景色を取得または設定します。
        /// </summary>
        public Color UnTebanPlayerNameBackgroundColor
        {
            get { return GetValue<Color>("UnTebanPlayerNameBackgroundColor"); }
            set { SetValue("UnTebanPlayerNameBackgroundColor", value); }
        }

        /// <summary>
        /// 時間部分の背景色を取得または設定します。
        /// </summary>
        public Color TimeBackgroundColor
        {
            get { return GetValue<Color>("TimeBackgroundColor"); }
            set { SetValue("TimeBackgroundColor", value); }
        }

        /// <summary>
        /// 盤全体が描画される領域を取得します。
        /// </summary>
        public RectangleF BoardBounds
        {
            get;
            private set;
        }
        
        /// <summary>
        /// マス全体の領域を取得します。
        /// </summary>
        public RectangleF BoardSquareBounds
        {
            get;
            private set;
        }

        /// <summary>
        /// １マスのサイズを取得します。
        /// </summary>
        public SizeF SquareSize
        {
            get;
            private set;
        }
        #endregion

        #region エフェクトなどの追加・削除
        /// <summary>
        /// エフェクトを追加します。
        /// </summary>
        public void AddEffect(EffectObject effect)
        {
            if (effect == null)
            {
                return;
            }

            Children.Add(effect);
        }

        /// <summary>
        /// エフェクトを削除します。
        /// </summary>
        public void RemoveEffect(EffectObject effect)
        {
            if (effect == null)
            {
                return;
            }

            Children.Remove(effect);
        }
        #endregion

        /// <summary>
        /// 各フレームごとに呼ばれます。
        /// </summary>
        protected override void OnEnterFrame(EnterFrameEventArgs e)
        {
            base.OnEnterFrame(e);
            var renderBuffer = (GL.RenderBuffer)e.StateObject;

            // 自動再生の更新を行います。
            if (this.autoPlay != null)
            {
                if (!this.autoPlay.Update(e.ElapsedTime))
                {
                    StopAutoPlay();
                }
            }

            // 盤
            renderBuffer.AddRender(
                this.boardTexture, BlendType.Diffuse,
                BoardBounds, Transform,
                ShogiZOrder.BoardZ, BoardOpacity);

            // 先手と後手の駒台と駒箱
            for (var index = 0; index < 3; ++index)
            {
                AddRenderPieceBox(renderBuffer, index);
            }

            // 盤上の駒をすべて描画登録します。
            AddRenderPieceAll(renderBuffer);
        }

        /// <summary>
        /// indexに対応した駒台を描画します。
        /// </summary>
        /// <param name="index">0なら駒箱、1なら先手用、2なら後手用の駒台となります。</param>
        private void AddRenderPieceBox(GL.RenderBuffer renderBuffer, int index)
        {
            // テクスチャがないときは帰ります。
            if (this.pieceTexture == null || this.pieceTexture.TextureName == 0)
            {
                return;
            }

            // 駒箱の場合はキャンセルするかもしれません
            if (index == 0 && !IsKomaBoxVisible)
            {
                return;
            }

            // 駒箱テクスチャ
            renderBuffer.AddRender(
                this.pieceBoxTexture, BlendType.Diffuse,
                this.pieceBoxBounds[index], Transform,
                ShogiZOrder.BoardZ,  BoardOpacity);

            // 駒台の上に対局者名を描画します。
            {
                var y = (index == 2 ?
                    this.pieceBoxBounds[index].Bottom - 5 - 15 :
                    this.pieceBoxBounds[index].Top + 5);
                var bounds = new RectangleF(
                    this.pieceBoxBounds[index].Left + 5, y,
                    this.pieceBoxBounds[index].Width - 10, 15);

                // 名前の背景に色をつけます。
                var color = (
                    Board.Turn == (BWType)index ?
                    TebanPlayerNameBackgroundColor :
                    UnTebanPlayerNameBackgroundColor);
                renderBuffer.AddRender(
                    BlendType.Diffuse, bounds, Transform,
                    color, ShogiZOrder.PostBoardZ);

                // 対局者名を描画
                var name = (
                    index == 1 ? BlackPlayerName :
                    index == 2 ? WhitePlayerName :
                    "駒箱");
                this.nameTexture[index].Text = name;
                this.nameTexture[index].Color = Color.White;
                if (!this.nameTexture[index].Font.Bold)
                {
                    this.nameTexture[index].Font = new Font(this.nameTexture[index].Font, FontStyle.Bold);
                }
                this.nameTexture[index].EdgeColor = Color.Black;
                this.nameTexture[index].EdgeLength = 2.0;
                bounds.Inflate(-4, 0);
                DrawString(
                    renderBuffer, this.nameTexture[index].Texture,
                    bounds, ShogiZOrder.PostBoardZ);
            }

            // 合計時間や消費時間の描画を行います。
            // 局面編集中など駒箱が表示されているときは残り時間を表示しません。
            if (IsTimeVisible && !IsKomaBoxVisible)
            {
                var y = (index == 2 ?
                    this.pieceBoxBounds[index].Bottom :
                    this.pieceBoxBounds[index].Top - 15);
                var bounds = new RectangleF(
                    this.pieceBoxBounds[index].Left, y,
                    this.pieceBoxBounds[index].Width, 15);
                renderBuffer.AddRender(
                    BlendType.Diffuse, bounds, Transform,
                    TimeBackgroundColor, ShogiZOrder.PostBoardZ);

                // 消費時間などを描画
                // 時間のフォーマットは '消費時間 / 合計時間' となっています。
                var totalTime = (index == 1 ? BlackTotalTime : WhiteTotalTime);
                var time = (index == 1 ? BlackTime : WhiteTime);
                var str = string.Format(
                    "{0:000}:{1:00} / 000:00",
                    (int)time.TotalMinutes, time.Seconds,
                    (int)totalTime.TotalMinutes, totalTime.Seconds);

                this.timeTexture[index].Text = str;
                this.timeTexture[index].Color = Color.White;
                bounds.Inflate(-4, 0);
                DrawString(
                    renderBuffer, this.timeTexture[index].Texture,
                    bounds, ShogiZOrder.PostBoardZ);
            }
        }

        /// <summary>
        /// 盤上の全ての駒を描画します。
        /// </summary>
        private void AddRenderPieceAll(GL.RenderBuffer renderBuffer)
        {
            if (this.pieceTexture == null || this.pieceTexture.TextureName == 0)
            {
                return;
            }

            var mp = this.movingPiece;
            var mpSquare = (mp != null ? mp.Square : null);
            var mpPiece = (mp != null ? mp.BoardPiece : null);

            // 盤上の駒
            foreach (var sq in Board.AllSquares())
            {
                var piece = Board[sq];
                if (piece != null && sq != mpSquare)
                {
                    var cpos = SquareToPoint(sq);
                    AddRenderPiece(
                        renderBuffer, piece, cpos, ShogiZOrder.PieceZ);
                }
            }

            // 先手・後手の駒台上の駒を更新します。
            var handList =
                from bw in EnumEx.GetValues<BWType>()
                from pc in EnumEx.GetValues<PieceType>()
                where bw != BWType.None || IsKomaBoxVisible
                where pc != PieceType.None
                let piece = new BoardPiece(pc, bw)
                let count = GetHandCount(pc, bw)
                let subCount = (mpSquare == null && mpPiece == piece ? 1 : 0)
                //where count - subCount > 0
                select new
                {
                    Piece = piece,
                    Count = count - subCount,
                };
            foreach (var hand in handList)
            {
                var cpos = HandPieceToPoint(hand.Piece);
                AddRenderPiece(
                    renderBuffer, hand.Piece, cpos, ShogiZOrder.PieceZ);
            }

            // 移動中の駒を描画します。
            if (mp != null)
            {
                AddRenderPiece(
                    renderBuffer, mp.BoardPiece, mp.Center,
                    ShogiZOrder.MovingPieceZ);
            }
        }

        /// <summary>
        /// 駒の描画を行います。
        /// </summary>
        private void AddRenderPiece(GL.RenderBuffer renderBuffer, BoardPiece piece,
                                    PointF cpos, double zorder)
        {
            if (this.pieceTexture == null || this.pieceTexture.TextureName == 0)
            {
                return;
            }

            var s = SquareSize;
            var bounds = new RectangleF(
                cpos.X - s.Width / 2, cpos.Y - s.Height / 2,
                s.Width, s.Height);
            var mesh = GetPieceMesh(piece);

            renderBuffer.AddRender(
                this.pieceTexture, BlendType.Diffuse,
                bounds, Transform, mesh, zorder);
        }

        /// <summary>
        /// 駒を描画するためのメッシュを取得します。
        /// </summary>
        private Mesh GetPieceMesh(BoardPiece piece)
        {
            if (piece == null || !piece.Validate())
            {
                throw new ArgumentException("piece");
            }

            Mesh mesh;
            if (!this.pieceMeshDic.TryGetValue(piece, out mesh))
            {
                return null;
            }

            return mesh;
        }

        private void DrawString(GL.RenderBuffer renderBuffer, GL.Texture texture,
                                RectangleF bounds, double zorder)
        {
            if (texture.TextureName != 0)
            {
                var r = (float)texture.OriginalWidth / texture.OriginalHeight;
                var br = (float)bounds.Width / bounds.Height;
                var w = (r >= br ? bounds.Width : bounds.Height * r);
                var h = (r >= br ? bounds.Width / r : bounds.Height);

                var result = new RectangleF(
                    (bounds.Left + bounds.Right) / 2 - w / 2,
                    (bounds.Top + bounds.Bottom) / 2 - h / 2,
                    w, h);
                renderBuffer.AddRender(
                    texture, BlendType.Diffuse,
                    result, Transform, zorder, 1.0);
            }
        }
    }
}
