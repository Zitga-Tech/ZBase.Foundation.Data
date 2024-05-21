using UnityEngine;
using ZBase.Foundation.Data;

namespace Samples
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

            var rowRef = table.GetRowByRef(id);
            Debug.Log($"rowRef = {rowRef.IsValid}");

            if (rowRef.IsValid)
            {
                ref readonly var data = ref rowRef.GetValueByRef();
                Debug.Log($"name = {data.Name}; hp = {data.Stat.Hp}; atk = {data.Stat.Atk}");
            }
        }

        private void OnDestroy()
        {
            _db.Deinitialize();
        }
    }
}
