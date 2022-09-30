using System;
using System.IO.Packaging;
using System.Linq;

namespace OpenXmlFileViewer.Extensions
{
    public static class ZipPackageExtensions
    {
        public static bool TryDeletePartAndRelationships(this ZipPackage package, Uri nodeToDelete)
        {
            if (!package.PartExists(nodeToDelete))
                return false;

            package
                .GetRelationships()
                .Where(r => r.TargetUri == nodeToDelete)
                .ToList()
                .ForEach(r => package.DeleteRelationship(r.Id));

            package.DeletePart(nodeToDelete);
            return true;
        }
    }
}
