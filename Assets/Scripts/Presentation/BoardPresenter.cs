using System;
using WarChess.Domain;
using WarChess.Application;

namespace WarChess.Presentation
{
    // 把行动预览转换为棋盘 View 可以显示的高亮类型
    public sealed class BoardPresenter
    {
        private readonly IBoardView _view;
        public BoardPresenter(IBoardView view)
        {
            _view = view;
        }

        public void Show(ActionPreview preview, UnitActionType actionType)
        {
            // 每次展示前清空旧高亮，确保画面完全由最新预览状态推导
            _view.ClearHighlights();
            if (preview == null || !preview.IsAllowed) return;

            BoardHighlight hightlights;
            switch (actionType)
            {
                case UnitActionType.Attack:
                    hightlights = BoardHighlight.Attack;
                    break;
                case UnitActionType.Defend:
                    hightlights = BoardHighlight.Defend;
                    break;
                default:
                    hightlights = BoardHighlight.Move;
                    break;
            }
            _view.ShowHighlights(preview.Target, hightlights);
        }

        public void Clear()
        {
            _view.ClearHighlights();
        }
    }
}