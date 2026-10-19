using System.Collections.Generic;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Podświetlanie obiektu przez przyciemnienie lub rozjaśnienie koloru wszystkich jego rendererów.
    /// Nie tworzy kopii materiałów: kolor nadpisuje przez <see cref="MaterialPropertyBlock"/>.
    /// </summary>
    public sealed class Tint
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<(Renderer renderer, Color baseColor)> _renderers = new List<(Renderer, Color)>();
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public Tint(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var material = renderer.sharedMaterial;
                if (material != null && material.HasProperty(BaseColorId))
                    _renderers.Add((renderer, material.GetColor(BaseColorId)));
            }
        }

        /// <summary>Miesza kolor bazowy z <paramref name="color"/> w proporcji <paramref name="amount"/> (0 = brak).</summary>
        public void Apply(Color color, float amount)
        {
            foreach (var (renderer, baseColor) in _renderers)
            {
                renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, Color.Lerp(baseColor, color, amount));
                renderer.SetPropertyBlock(_block);
            }
        }

        public void Clear()
        {
            foreach (var (renderer, _) in _renderers)
                renderer.SetPropertyBlock(null);
        }
    }
}
