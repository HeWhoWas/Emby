﻿using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using CommonIO;

namespace MediaBrowser.Providers.Photos
{
    class PhotoAlbumMetadataService : MetadataService<PhotoAlbum, ItemLookupInfo>
    {
        public PhotoAlbumMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem, userDataManager, libraryManager)
        {
        }

        protected override void MergeData(MetadataResult<PhotoAlbum> source, MetadataResult<PhotoAlbum> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }
    }
}
