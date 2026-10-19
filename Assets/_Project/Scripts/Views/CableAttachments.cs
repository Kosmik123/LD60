using System.Collections.Generic;
using UnityEngine;

namespace SpaghettiWires.Views
{
    /// <summary>
    /// Punkty przyczepu kabli na modelu węzła i ich zajętość. Punkty to dzieci o nazwach
    /// zaczynających się od „CableAnchor”. Kabel dostaje punkt przy przyczepieniu i trzyma go,
    /// dopóki nie zostanie odczepiony.
    /// Ten sam komponent odpowiada, czy węzeł ma jeszcze miejsce i który punkt jest wolny (<see cref="AnchorSlots"/>).
    /// </summary>
    public sealed class CableAttachments : MonoBehaviour
    {
        public enum AnchorChoice
        {
            /// <summary>Losowy wolny punkt (słup). Losowanie zależy od Id kabla, więc jest powtarzalne.</summary>
            Random,

            /// <summary>Wolny punkt najbliższy drugiemu końcowi kabla (dom, centrala).</summary>
            Nearest,
        }

        private const string AnchorPrefix = "CableAnchor";

        [SerializeField] private AnchorChoice choice = AnchorChoice.Nearest;
        [Tooltip("Pojemność węzła w miejscach (kabel zajmuje ⌈√żył⌉). 0 = bez limitu.")]
        [Min(0)] [SerializeField] private int capacity = AnchorSlots.Unlimited;
        [Tooltip("Ile kabli zmieści się na jednym punkcie przyczepu. 0 = bez limitu.")]
        [Min(0)] [SerializeField] private int placesPerAnchor = AnchorSlots.Unlimited;

        private readonly List<Transform> _anchors = new List<Transform>();
        private readonly List<int> _candidates = new List<int>();
        private AnchorSlots _slots;

        public AnchorSlots Slots
        {
            get
            {
                EnsureInitialized();
                return _slots;
            }
        }

        /// <summary>Ustawienia dla skryptów budujących modele. Działa tylko przed pierwszym użyciem.</summary>
        public void Configure(AnchorChoice anchorChoice, int nodeCapacity, int anchorPlaces)
        {
            choice = anchorChoice;
            capacity = nodeCapacity;
            placesPerAnchor = anchorPlaces;
        }

        public bool CanAttach(int pairs) => Slots.CanAttach(pairs);

        /// <summary>Przyczepia kabel do wolnego punktu i zwraca jego pozycję.</summary>
        public Vector3 Attach(int cableId, int pairs, Vector3 towards)
        {
            int anchor = ChooseAnchor(cableId, towards);
            Slots.Attach(cableId, pairs, anchor);
            return _anchors[anchor].position;
        }

        public void Detach(int cableId) => Slots.Detach(cableId);

        public Vector3 GetAnchorPosition(int cableId)
        {
            if (!Slots.TryGetAnchor(cableId, out int anchor))
                throw new KeyNotFoundException($"Cable #{cableId} is not attached to {name}.");
            return _anchors[anchor].position;
        }

        /// <summary>
        /// Gdzie zaczepi się kabel o podanym Id, bez rezerwowania miejsca: do podglądu i sprawdzania przed położeniem.
        /// Wybiera punkt tak samo jak <see cref="Attach"/>, więc przy niezmienionej zajętości to dokładnie ten sam punkt.
        /// Gdy wolnych punktów nie ma, zwraca najbliższy (kabla i tak nie da się wtedy położyć).
        /// </summary>
        public Vector3 PreviewPosition(int cableId, Vector3 towards)
        {
            int anchor = ChooseAnchor(cableId, towards);
            return _anchors[anchor >= 0 ? anchor : NearestAnchor(towards)].position;
        }

        private int ChooseAnchor(int cableId, Vector3 towards)
        {
            EnsureInitialized();
            if (choice == AnchorChoice.Nearest)
                return NearestFreeAnchor(towards);

            _candidates.Clear();
            for (int i = 0; i < _anchors.Count; i++)
            {
                if (_slots.IsAnchorFree(i))
                    _candidates.Add(i);
            }
            return _candidates.Count == 0 ? -1 : _candidates[new System.Random(cableId).Next(_candidates.Count)];
        }

        private int NearestFreeAnchor(Vector3 towards)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _anchors.Count; i++)
            {
                float distance = (_anchors[i].position - towards).sqrMagnitude;
                if (_slots.IsAnchorFree(i) && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        private int NearestAnchor(Vector3 towards)
        {
            int best = 0;
            for (int i = 1; i < _anchors.Count; i++)
            {
                if ((_anchors[i].position - towards).sqrMagnitude < (_anchors[best].position - towards).sqrMagnitude)
                    best = i;
            }
            return best;
        }

        // Leniwie, a nie w Awake: widoki bywają tworzone też w trybie edycji, gdzie Awake się nie wywołuje.
        private void EnsureInitialized()
        {
            if (_slots != null)
                return;

            foreach (var child in GetComponentsInChildren<Transform>())
            {
                if (child.name.StartsWith(AnchorPrefix))
                    _anchors.Add(child);
            }

            if (_anchors.Count == 0)
            {
                Debug.LogWarning($"{name} has no {AnchorPrefix} points, cables will attach to its origin.", this);
                _anchors.Add(transform);
            }

            _slots = new AnchorSlots(_anchors.Count, capacity, placesPerAnchor);
        }
    }
}
