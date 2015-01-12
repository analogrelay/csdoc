using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;

namespace NetDoc
{
    public class Program
    {
        private readonly ICache _cache;
        private readonly IFileWatcher _watcher;
        private readonly IServiceProvider _services;
        private readonly ICacheContextAccessor _cacheContextAccessor;
        private readonly IAssemblyLoadContextFactory _assemblyLoadContextFactory;
        private readonly INamedCacheDependencyProvider _namedCacheDependencyProvider;

        private readonly IApplicationEnvironment _env;
        private readonly ILibraryManager _libraryManager;
        private readonly IProjectResolver _projectResolver;

        public Program(
            ICache cache,
            IFileWatcher watcher,
            IServiceProvider services,
            ICacheContextAccessor cacheContextAccessor,
            IAssemblyLoadContextFactory assemblyLoadContextFactory,
            INamedCacheDependencyProvider namedCacheDependencyProvider,
            IProjectResolver projectResolver,
            ILibraryManager libraryManager,
            IApplicationEnvironment env)
        {
            _cache = cache;
            _watcher = watcher;
            _services = services;
            _cacheContextAccessor = cacheContextAccessor;
            _assemblyLoadContextFactory = assemblyLoadContextFactory;
            _namedCacheDependencyProvider = namedCacheDependencyProvider;

            _projectResolver = projectResolver;
            _libraryManager = libraryManager;
            _env = env;
        }

        public int Main(string[] args)
        {
            Project prj;
            if (!_projectResolver.TryResolveProject(_env.ApplicationName, out prj)) {
                Console.Error.WriteLine("Failed to resolve project");
                return 1;
            }

            var rootCacheContext = new CacheContext(new object(), _ => { });
            _cacheContextAccessor.Current = rootCacheContext;

            var compiler = new RoslynCompiler(
                _cache,
                _cacheContextAccessor,
                _namedCacheDependencyProvider,
                _assemblyLoadContextFactory,
                _watcher,
                _services);
            var key = new LibraryKey()
            {
                Aspect = null,
                Name = prj.Name,
                Configuration = _env.Configuration,
                TargetFramework = _env.RuntimeFramework
            };
            var export = _libraryManager.GetLibraryExport(prj.Name);
            var refr = export.MetadataReferences
                .OfType<IMetadataProjectReference>()
                .FirstOrDefault(m => string.Equals(m.Name, prj.Name, StringComparison.OrdinalIgnoreCase));
            if (refr == null)
            {
                Console.Error.WriteLine("Unable to get metadata reference");
                return 2;
            }

            List<IMetadataReference> references = new List<IMetadataReference>();
            var result = compiler.CompileProject(
                prj,
                key,
                export.MetadataReferences,
                export.SourceReferences,
                references);

            // Visit syntax trees
            var docVisitor = new DocumentationVisitor();
            result.Compilation.Assembly.Accept(docVisitor);

            return 0;
        }

        private class LibraryKey : ILibraryKey
        {
            public string Aspect { get; set; }
            public string Configuration { get; set; }
            public string Name { get; set; }
            public FrameworkName TargetFramework { get; set; }
        }
    }
}
