using UnityEngine;
using ZBase.Foundation.Data;

namespace ZBase.Foundation.Data.Samples
{
    public class SampleBehaviour : MonoBehaviour
    {
        [SerializeField]
        private DatabaseAsset _db;

        private void Start()
        {
            _db.Initialize();
            _db.TryGetDataTableAsset<HeroDataTableAsset>(out var table);

            var id = new IdData {
                Kind = EntityKind.Hero,
                Id = 1,
            };

            var entryRef = table.GetEntryByRef(id);
            Debug.Log($"valid = {entryRef.IsValid}");

            if (entryRef.IsValid)
            {
                ref readonly var data = ref entryRef.GetValueByRef();
                Debug.Log($"name = {data.Name}; hp = {data.Stat.Hp}; atk = {data.Stat.Atk}");
            }
        }

        private void OnDestroy()
        {
            _db.Deinitialize();
        }
    }
}
