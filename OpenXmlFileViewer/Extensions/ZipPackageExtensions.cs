using System;
using System.IO.Packaging;
using System.Linq;

namespace OpenXmlFileViewer.Extensions
{
    public static class ZipPackageExtensions
    {
        public static void DeletePartAndRelationships(this ZipPackage package, Uri nodeToDelete)
        {
            var part = package.GetPart(nodeToDelete);
            package
                .GetRelationships()
                .Where(r => r.TargetUri == nodeToDelete)
                .ToList()
                .ForEach(r => package.DeleteRelationship(r.Id));

            package.DeletePart(nodeToDelete);
        }
    }
}
