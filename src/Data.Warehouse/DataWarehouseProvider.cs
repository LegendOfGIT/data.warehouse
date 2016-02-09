using System.Collections.Generic;

namespace Data.Warehouse
{
    public interface DataWarehouseProvider
    {
        IEnumerable<Dictionary<string, IEnumerable<object>>> DigInformation(string question);
        void StoreInformation(Dictionary<string, IEnumerable<object>> information);
    }
}
