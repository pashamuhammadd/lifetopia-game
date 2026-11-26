using UnityEngine;

namespace Lifetopia.Npc
{
    /// <summary>
    /// Static NPC art: one idle sprite per direction plus a dialogue portrait.
    /// Assets that point to original (local-only) artwork live in the git-excluded LocalArt folder and are
    /// referenced only from local scenes; the public scene leaves the field empty and shows a placeholder.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/NPC/NPC Visual Set", fileName = "NpcVisualSet")]
    public sealed class NpcVisualSet : ScriptableObject
    {
        [SerializeField] private string npcId = "";
        [SerializeField] private string displayName = "";
        [Tooltip("Idle sprites, pivot at the feet (0.5, 0.125), PPU 64.")]
        [SerializeField] private Sprite down;
        [SerializeField] private Sprite left;
        [SerializeField] private Sprite right;
        [SerializeField] private Sprite up;
        [SerializeField] private Sprite portrait;

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public bool IsComplete => down != null && left != null && right != null && up != null;

        public Sprite For(Player.FacingDirection facing)
        {
            switch (facing)
            {
                case Player.FacingDirection.Left: return left;
                case Player.FacingDirection.Right: return right;
                case Player.FacingDirection.Up: return up;
                default: return down;
            }
        }
    }
}
