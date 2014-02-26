using System;
using System.Collections.Generic;
using System.Linq;
using Sparql;

namespace TrueRdfViewer
{
    public static class RPackComplexExtensionInt
    {
        public static IEnumerable<RPackInt> OptionalGroup(this IEnumerable<RPackInt> pack, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> group, params short[] changedVariables)
        {
            var packArray = pack as RPackInt[] ?? pack.ToArray();
            var optionalGroup = @group(packArray).ToArray();
 
            if (optionalGroup.Length!=0) return optionalGroup;

            return packArray.Select(pk =>
            {
                for (int i = 0; i < changedVariables.Length; i++)
                    pk.Set(changedVariables[i], string.Empty);
                return pk;
            });
        }

        public static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> Optional(this GraphSelectorAndParams graphSelector)
        {
            return packs =>
            {
                var packArray =packs as RPackInt[] ?? packs.ToArray();
                var optionalGroup = graphSelector.GraphSelector(packArray).ToArray();

                if (optionalGroup.Length != 0) return optionalGroup;

                return packArray.Select(pk =>
                {
                    for (int i = 0; i < graphSelector.Parameters.Count; i++)
                        pk.Set(graphSelector.Parameters[i], string.Empty);
                    return pk;
                });
            };
        }

        public static IEnumerable<RPackInt> Union(this IEnumerable<RPackInt> pack,
            params Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>>[] groups)
        {
            return groups.SelectMany(group => group(pack));
        }
    }

}