namespace ScrewTurn.Wiki.BackupRestore 
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web.Script.Serialization;
    using ScrewTurn.Wiki.PluginFramework;
    using ScrewTurn.Wiki.AclEngine;
    using Ionic.Zip;

    /// <summary>
    /// Implements export and import of wiki pages. Based on BackupRestore on https://bitbucket.org/screwturn/screwturn-wiki/overview
    /// and BackupRestore of 4th version ScrewTurnWiki https://stw.codeplex.com/SourceControl/latest?branch=v4.
    /// </summary>
    public static class BackupRestore
    {
        /// <summary>
        /// Version.
        /// </summary>
        private const string BACKUP_RESTORE_UTILITY_VERSION = "1.0";

        /// <summary>
        /// Generate version file.
        /// </summary>
        /// <param name="backupName"></param>
        /// <returns></returns>
        private static VersionFile generateVersionFile(string backupName)
        {
            return new VersionFile()
            {
                BackupRestoreVersion = BACKUP_RESTORE_UTILITY_VERSION,
                WikiVersion = typeof(BackupRestore).Assembly.GetName().Version.ToString(),
                BackupName = backupName
            };
        }

        #region Backuping Additional

        /// <summary>
        /// Backups all prividers.
        /// </summary>
        /// <param name="backupZipFileName">Zip file name.</param>
        /// <param name="plugins">Available plugins.</param>
        /// <param name="settingsStorageProvider"></param>
        /// <param name="pagesStorageProviders"></param>
        /// <param name="usersStorageProviders"></param>
        /// <param name="filesStorageProviders"></param>
        /// <param name="nspacesList">List of namespaces to backup.</param>
        /// <param name="catList">List of categories to backup.</param>
        /// <returns><c>true</c> if backup succeded.</returns>
        public static bool BackupAll(string backupZipFileName, string[] plugins, ISettingsStorageProviderV30 settingsStorageProvider, IPagesStorageProviderV30[] pagesStorageProviders, 
            IUsersStorageProviderV30[] usersStorageProviders, IFilesStorageProviderV30[] filesStorageProviders, List<string> nspacesList, List<string> catList)
        {
            string tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            using (ZipFile backupZipFile = new ZipFile(backupZipFileName))
            {

                // Looks for namespaces.
                List<string> namespaces = new List<string>();
                foreach (IPagesStorageProviderV30 pagesStorageProvider in pagesStorageProviders)
                {
                    foreach (NamespaceInfo ns in pagesStorageProvider.GetNamespaces())
                    {
                        namespaces.Add(ns.Name);
                    }
                }

                //// Backups settings (uncomment when needed).
                //string zipSettingsBackup = Path.Combine(tempPath, "SettingsBackup-" + settingsStorageProvider.GetType().FullName + ".zip");
                //BackupSettingsStorageProvider(zipSettingsBackup, settingsStorageProvider, namespaces.ToArray(), plugins);
                //backupZipFile.AddFile(zipSettingsBackup, "");

                // Backups pages and attachments.
                foreach (IPagesStorageProviderV30 pagesStorageProvider in pagesStorageProviders)
                {
                    string zipPagesBackup = Path.Combine(tempPath, "PagesBackup-" + pagesStorageProvider.GetType().FullName + ".zip");
                    BackupPagesStorageProvider(zipPagesBackup, pagesStorageProvider, filesStorageProviders.FirstOrDefault(), nspacesList, catList);
                    backupZipFile.AddFile(zipPagesBackup, "");
                }

                //// Backups users informations (uncomment when needed).
                //foreach (IUsersStorageProviderV30 usersStorageProvider in usersStorageProviders)
                //{
                //    string zipUsersProvidersBackup = Path.Combine(tempPath, "UsersBackup-" + usersStorageProvider.GetType().FullName + ".zip");
                //    BackupUsersStorageProvider(zipUsersProvidersBackup, usersStorageProvider);
                //    backupZipFile.AddFile(zipUsersProvidersBackup, "");
                //}

                // Backups files.
                foreach (IFilesStorageProviderV30 filesStorageProvider in filesStorageProviders)
                {
                    string zipFilesProviderBackup = Path.Combine(tempPath, "FilesBackup-" + filesStorageProvider.GetType().FullName + ".zip");
                    BackupFilesStorageProvider(zipFilesProviderBackup, filesStorageProvider, pagesStorageProviders);
                    backupZipFile.AddFile(zipFilesProviderBackup, "");
                }
                backupZipFile.Save();
            }

            Directory.Delete(tempPath, true);
            return true;
        }

        /// <summary>
        /// Backups settings.
        /// </summary>
        /// <param name="zipFileName"></param>
        /// <param name="settingsStorageProvider"></param>
        /// <param name="knownNamespaces">The currently known page namespaces.</param>
        /// <param name="knownPlugins">The currently known plugins.</param>
        /// <returns><c>true</c> если экспорт был успешно завершен.</returns>
        public static bool BackupSettingsStorageProvider(string zipFileName, ISettingsStorageProviderV30 settingsStorageProvider, string[] knownNamespaces, string[] knownPlugins)
        {
            SettingsBackup settingsBackup = new SettingsBackup();

            // Settings
            settingsBackup.Settings = (Dictionary<string, string>)settingsStorageProvider.GetAllSettings();

            // Plugins Status and Configuration
            settingsBackup.PluginsFileNames = knownPlugins.ToList();
            Dictionary<string, bool> pluginsStatus = new Dictionary<string, bool>();
            Dictionary<string, string> pluginsConfiguration = new Dictionary<string, string>();
            foreach (string plugin in knownPlugins)
            {
                pluginsStatus[plugin] = settingsStorageProvider.GetPluginStatus(plugin);
                pluginsConfiguration[plugin] = settingsStorageProvider.GetPluginConfiguration(plugin);
            }
            settingsBackup.PluginsStatus = pluginsStatus;
            settingsBackup.PluginsConfiguration = pluginsConfiguration;

            // Metadata
            List<MetaData> metadataList = new List<MetaData>();
            // Meta-data (global)
            metadataList.Add(new MetaData()
            {
                Item = MetaDataItem.AccountActivationMessage,
                Tag = null,
                Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.AccountActivationMessage, null)
            });
            metadataList.Add(new MetaData() { Item = MetaDataItem.PasswordResetProcedureMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PasswordResetProcedureMessage, null) });
            metadataList.Add(new MetaData() { Item = MetaDataItem.LoginNotice, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.LoginNotice, null) });
            metadataList.Add(new MetaData() { Item = MetaDataItem.PageChangeMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageChangeMessage, null) });
            metadataList.Add(new MetaData() { Item = MetaDataItem.DiscussionChangeMessage, Tag = null, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.DiscussionChangeMessage, null) });
            // Meta-data (ns-specific)
            List<string> namespacesToProcess = new List<string>();
            namespacesToProcess.Add("");
            namespacesToProcess.AddRange(knownNamespaces);
            foreach (string nspace in namespacesToProcess)
            {
                metadataList.Add(new MetaData() { Item = MetaDataItem.EditNotice, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.EditNotice, nspace) });
                metadataList.Add(new MetaData() { Item = MetaDataItem.Footer, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Footer, nspace) });
                metadataList.Add(new MetaData() { Item = MetaDataItem.Header, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Header, nspace) });
                metadataList.Add(new MetaData() { Item = MetaDataItem.HtmlHead, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.HtmlHead, nspace) });
                metadataList.Add(new MetaData() { Item = MetaDataItem.PageFooter, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageFooter, nspace) });
                metadataList.Add(new MetaData() { Item = MetaDataItem.PageHeader, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.PageHeader, nspace) });
                metadataList.Add(new MetaData() { Item = MetaDataItem.Sidebar, Tag = nspace, Content = settingsStorageProvider.GetMetaDataItem(MetaDataItem.Sidebar, nspace) });
            }
            settingsBackup.Metadata = metadataList;

            // RecentChanges
            settingsBackup.RecentChanges = settingsStorageProvider.GetRecentChanges().ToList();

            // OutgoingLinks
            settingsBackup.OutgoingLinks = (Dictionary<string, string[]>)settingsStorageProvider.GetAllOutgoingLinks();

            // ACLEntries
            AclEntry[] aclEntries = settingsStorageProvider.AclManager.RetrieveAllEntries();
            settingsBackup.AclEntries = new List<AclEntryBackup>(aclEntries.Length);
            foreach (AclEntry aclEntry in aclEntries)
            {
                settingsBackup.AclEntries.Add(new AclEntryBackup()
                {
                    Action = aclEntry.Action,
                    Resource = aclEntry.Resource,
                    Subject = aclEntry.Subject,
                    Value = aclEntry.Value
                });
            }

            JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
            javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

            string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            FileStream tempFile = File.Create(Path.Combine(tempDir, "Settings.json"));
            byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(settingsBackup));
            tempFile.Write(buffer, 0, buffer.Length);
            tempFile.Close();

            tempFile = File.Create(Path.Combine(tempDir, "Version.json"));
            buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Settings")));
            tempFile.Write(buffer, 0, buffer.Length);
            tempFile.Close();

            using (ZipFile zipFile = new ZipFile())
            {
                zipFile.AddDirectory(tempDir, "");
                zipFile.Save(zipFileName);
            }
            Directory.Delete(tempDir, true);

            return true;
        }



        /// <summary>
        /// Backups users information.
        /// </summary>
        /// <param name="zipFileName"></param>
        /// <param name="usersStorageProvider"></param>
        /// <returns><c>true</c> if export succeeded.</returns>
        public static bool BackupUsersStorageProvider(string zipFileName, IUsersStorageProviderV30 usersStorageProvider)
        {
            JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
            javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

            string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            // Backup users
            UserInfo[] users = usersStorageProvider.GetUsers();
            List<UserBackup> usersBackup = new List<UserBackup>(users.Length);
            foreach (UserInfo user in users)
            {
                usersBackup.Add(new UserBackup()
                {
                    Username = user.Username,
                    Active = user.Active,
                    DateTime = user.DateTime,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    Groups = user.Groups,
                    UserData = usersStorageProvider.RetrieveAllUserData(user)
                });
            }
            FileStream tempFile = File.Create(Path.Combine(tempDir, "Users.json"));
            byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(usersBackup));
            tempFile.Write(buffer, 0, buffer.Length);
            tempFile.Close();

            // Backup UserGroups
            UserGroup[] userGroups = usersStorageProvider.GetUserGroups();
            List<UserGroupBackup> userGroupsBackup = new List<UserGroupBackup>(userGroups.Length);
            foreach (UserGroup userGroup in userGroups)
            {
                userGroupsBackup.Add(new UserGroupBackup()
                {
                    Name = userGroup.Name,
                    Description = userGroup.Description
                });
            }

            tempFile = File.Create(Path.Combine(tempDir, "Groups.json"));
            buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(userGroupsBackup));
            tempFile.Write(buffer, 0, buffer.Length);
            tempFile.Close();

            tempFile = File.Create(Path.Combine(tempDir, "Version.json"));
            buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Users")));
            tempFile.Write(buffer, 0, buffer.Length);
            tempFile.Close();


            using (ZipFile zipFile = new ZipFile())
            {
                zipFile.AddDirectory(tempDir, "");
                zipFile.Save(zipFileName);
            }
            Directory.Delete(tempDir, true);

            return true;
        }



        #endregion

        #region Backuping Pages and Attachemnts

        /// <summary>
        /// Backups pages and attachments.
        /// </summary>
        /// <param name="zipFileName"></param>
        /// <param name="pagesStorageProvider"></param>
        /// <param name="filesStorageProvider"></param>
        /// <param name="nspacesList">List of namespaces to backup.</param>
        /// <param name="catList">List of categories to backup.</param>
        /// <returns><c>true</c> if backup was successful.</returns>
        public static bool BackupPagesStorageProvider(string zipFileName, IPagesStorageProviderV30 pagesStorageProvider, 
            IFilesStorageProviderV30 filesStorageProvider, List<string> nspacesList, List<string> catList)
        {
            JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
            javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

            string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            // Populate namespaces list from a list of their names typed on a form.
            // If a textbox on a form is empty, list will be populated with all namespaces.
            List<NamespaceInfo> nspaces = new List<NamespaceInfo>();
            if (nspacesList.Count != 0)
            {
                nspaces.AddRange(nspacesList.Select(pagesStorageProvider.GetNamespace));
            }
            else
            {
                nspaces = new List<NamespaceInfo>(pagesStorageProvider.GetNamespaces());
                nspaces.Add(null);
            }
            List<NamespaceBackup> namespaceBackupList = new List<NamespaceBackup>(nspaces.Count);

            foreach (NamespaceInfo nspace in nspaces)
            {
                // Backup categories.
                CategoryInfo[] categories = pagesStorageProvider.GetCategories(nspace);
                List<CategoryBackup> categoriesBackup = new List<CategoryBackup>(categories.Length);
                foreach (CategoryInfo category in categories)
                {
                    // Add this category to the categoriesBackup list.
                    categoriesBackup.Add(new CategoryBackup()
                    {
                        FullName = category.FullName,
                        Pages = category.Pages
                    });
                }

                // Backup NavigationPaths.
                NavigationPath[] navigationPaths = pagesStorageProvider.GetNavigationPaths(nspace);
                List<NavigationPathBackup> navigationPathsBackup = new List<NavigationPathBackup>(navigationPaths.Length);
                foreach (NavigationPath navigationPath in navigationPaths)
                {
                    navigationPathsBackup.Add(new NavigationPathBackup()
                    {
                        FullName = navigationPath.FullName,
                        Pages = navigationPath.Pages
                    });
                }

                // Add this namespace to the namespaceBackup list.
                namespaceBackupList.Add(new NamespaceBackup()
                {
                    Name = nspace == null ? string.Empty : nspace.Name,
                    DefaultPageFullName = nspace == null ? string.Empty : nspace.DefaultPage.FullName,
                    Categories = categoriesBackup,
                    NavigationPaths = navigationPathsBackup
                });

                // Populates list of pages to backup according to categories names typed on a form.
                // If textbox for categories is emmpty, list will be populated with all pages of a namespace.
                List<PageInfo> pagesToExport = new List<PageInfo>();
                if (catList.Count != 0)
                {
                    foreach (string catName in catList)
                    {
                        string catFullName = nspace != null ? string.Format("{0}.{1}", nspace.Name, catName) : catName;
                        CategoryInfo cat = categories.FirstOrDefault(c => c.FullName.Equals(catFullName));
                        if (cat != null)
                        {
                            pagesToExport.AddRange(cat.Pages.Select(Pages.FindPage));
                        }
                    }
                }
                else
                {
                    pagesToExport = new List<PageInfo>(pagesStorageProvider.GetPages(nspace));
                }
                foreach (PageInfo page in pagesToExport)
                {
                    PageContent pageContent = pagesStorageProvider.GetContent(page);
                    PageBackup pageBackup = new PageBackup();
                    pageBackup.NameSpace = nspace == null ? string.Empty : nspace.Name;
                    pageBackup.FullName = page.FullName;
                    pageBackup.CreationDateTime = page.CreationDateTime;
                    pageBackup.LastModified = pageContent.LastModified;
                    pageBackup.Content = pageContent.Content;
                    pageBackup.Comment = pageContent.Comment;
                    pageBackup.Description = pageContent.Description;
                    pageBackup.Keywords = pageContent.Keywords;
                    pageBackup.Title = pageContent.Title;
                    pageBackup.User = pageContent.User;
                    pageBackup.LinkedPages = pageContent.LinkedPages;
                    pageBackup.Categories = (from c in pagesStorageProvider.GetCategoriesForPage(page)
                        select c.FullName).ToArray();

                    // Backup the 100 most recent versions of the page.
                    List<PageRevisionBackup> pageContentBackupList = new List<PageRevisionBackup>();
                    int[] revisions = pagesStorageProvider.GetBackups(page);
                    for (int i = revisions.Length - 1; i > revisions.Length - 100 && i >= 0; i--)
                    {
                        PageContent pageRevision = pagesStorageProvider.GetBackupContent(page, revisions[i]);
                        PageRevisionBackup pageContentBackup = new PageRevisionBackup()
                        {
                            Revision = revisions[i],
                            Content = pageRevision.Content,
                            Comment = pageRevision.Comment,
                            Description = pageRevision.Description,
                            Keywords = pageRevision.Keywords,
                            Title = pageRevision.Title,
                            User = pageRevision.User,
                            LastModified = pageRevision.LastModified
                        };
                        pageContentBackupList.Add(pageContentBackup);
                     }
                     pageBackup.Revisions = pageContentBackupList;

                     // Backup draft of the page.
                     PageContent draft = pagesStorageProvider.GetDraft(page);
                     if (draft != null)
                     {
                        pageBackup.Draft = new PageRevisionBackup()
                        {
                            Content = draft.Content,
                            Comment = draft.Comment,
                            Description = draft.Description,
                            Keywords = draft.Keywords,
                            Title = draft.Title,
                            User = draft.User,
                            LastModified = draft.LastModified
                        };
                     }

                     // Backup all messages of the page.
                     List<MessageBackup> messageBackupList = new List<MessageBackup>();
                     foreach (Message message in pagesStorageProvider.GetMessages(page))
                     {
                        messageBackupList.Add(BackupMessage(message));
                     }
                     pageBackup.Messages = messageBackupList;

                     FileStream tempFile = File.Create(Path.Combine(tempDir, page.FullName + ".json"));
                     byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(pageBackup));
                     tempFile.Write(buffer, 0, buffer.Length);
                     tempFile.Close();

                     // Backup all attachments of the page.
                     List<AttachmentBackup> attachmentBackups = new List<AttachmentBackup>();
                     if (filesStorageProvider.ListPageAttachments(page).ToList().Count > 0)
                     {
                         string[] attachments = filesStorageProvider.ListPageAttachments(page);
                         List<AttachmentBackup> attachmentsBackup = new List<AttachmentBackup>(attachments.Length);

                         foreach (string attachment in attachments)
                         {
                             FileDetails attachmentDetails = filesStorageProvider.GetPageAttachmentDetails(page, attachment);
                             attachmentsBackup.Add(new AttachmentBackup()
                             {
                                 Name = attachment,
                                 PageFullName = page.FullName,
                                 LastModified = attachmentDetails.LastModified,
                                 Size = attachmentDetails.Size
                             });
                             using (MemoryStream stream = new MemoryStream())
                             {
                                 filesStorageProvider.RetrievePageAttachment(page, attachment, stream, false);
                                 stream.Seek(0, SeekOrigin.Begin);
                                 byte[] tempBuffer = new byte[stream.Length];
                                 stream.Read(tempBuffer, 0, (int)stream.Length);

                                 DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(tempDir, Path.Combine("__attachments", page.FullName)));
                                 tempFile = File.Create(Path.Combine(dir.FullName, attachment));
                                 tempFile.Write(tempBuffer, 0, tempBuffer.Length);
                                 tempFile.Close();
                             }
                         }
                         tempFile = File.Create(Path.Combine(tempDir, Path.Combine("__attachments", Path.Combine(page.FullName, "Attachments.json"))));
                         buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(attachmentsBackup));
                         tempFile.Write(buffer, 0, buffer.Length);
                         tempFile.Close();
                     }
                 }   
            }

            FileStream tempNamespacesFile = File.Create(Path.Combine(tempDir, "Namespaces.json"));
            byte[] namespacesBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(namespaceBackupList));
            tempNamespacesFile.Write(namespacesBuffer, 0, namespacesBuffer.Length);
            tempNamespacesFile.Close();

            // Backup content templates.
            ContentTemplate[] contentTemplates = pagesStorageProvider.GetContentTemplates();
            List<ContentTemplateBackup> contentTemplatesBackup = new List<ContentTemplateBackup>(contentTemplates.Length);
            foreach (ContentTemplate contentTemplate in contentTemplates)
            {
                contentTemplatesBackup.Add(new ContentTemplateBackup()
                {
                    Name = contentTemplate.Name,
                    Content = contentTemplate.Content
                });
            }
            FileStream tempContentTemplatesFile = File.Create(Path.Combine(tempDir, "ContentTemplates.json"));
            byte[] contentTemplateBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(contentTemplatesBackup));
            tempContentTemplatesFile.Write(contentTemplateBuffer, 0, contentTemplateBuffer.Length);
            tempContentTemplatesFile.Close();

            // Backup Snippets.
            Snippet[] snippets = pagesStorageProvider.GetSnippets();
            List<SnippetBackup> snippetsBackup = new List<SnippetBackup>(snippets.Length);
            foreach (Snippet snippet in snippets)
            {
                snippetsBackup.Add(new SnippetBackup()
                {
                    Name = snippet.Name,
                    Content = snippet.Content
                });
            }
            FileStream tempSnippetsFile = File.Create(Path.Combine(tempDir, "Snippets.json"));
            byte[] snippetBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(snippetsBackup));
            tempSnippetsFile.Write(snippetBuffer, 0, snippetBuffer.Length);
            tempSnippetsFile.Close();

            FileStream tempVersionFile = File.Create(Path.Combine(tempDir, "Version.json"));
            byte[] versionBuffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(generateVersionFile("Pages")));
            tempVersionFile.Write(versionBuffer, 0, versionBuffer.Length);
            tempVersionFile.Close();

            using (ZipFile zipFile = new ZipFile())
            {
                zipFile.AddDirectory(tempDir, "");
                zipFile.Save(zipFileName);
            }
            Directory.Delete(tempDir, true);

            return true;
        }

        /// <summary>
        /// Recursively backups all messages on a page.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Message backup.</returns>
        private static MessageBackup BackupMessage(Message message)
        {
            MessageBackup messageBackup = new MessageBackup()
            {
                Id = message.ID,
                Subject = message.Subject,
                Body = message.Body,
                DateTime = message.DateTime,
                Username = message.Username
            };
            List<MessageBackup> repliesBackup = new List<MessageBackup>(message.Replies.Length);
            foreach (Message reply in message.Replies)
            {
                repliesBackup.Add(BackupMessage(reply));
            }
            messageBackup.Replies = repliesBackup;
            return messageBackup;
        }

        #endregion

        #region Backuping Files

        /// <summary>
        /// Backups files.
        /// </summary>
        /// <param name="zipFileName"></param>
        /// <param name="filesStorageProvider"></param>
        /// <param name="pagesStorageProviders"></param>
        /// <returns><c>true</c> if backup was successful.</returns>
        public static bool BackupFilesStorageProvider(string zipFileName, IFilesStorageProviderV30 filesStorageProvider, IPagesStorageProviderV30[] pagesStorageProviders)
        {
            JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
            javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

            string tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            DirectoryBackup directoriesBackup = BackupDirectory(filesStorageProvider, tempDir, null);
            FileStream tempFile = File.Create(Path.Combine(tempDir, "Files.json"));
            byte[] buffer = Encoding.Unicode.GetBytes(javascriptSerializer.Serialize(directoriesBackup));
            tempFile.Write(buffer, 0, buffer.Length);
            tempFile.Close();

            using (ZipFile zipFile = new ZipFile())
            {
                zipFile.AddDirectory(tempDir, "");
                zipFile.Save(zipFileName);
            }
            Directory.Delete(tempDir, true);

            return true;
        }

        /// <summary>
        /// Recursively backups directories.
        /// </summary>
        /// <param name="filesStorageProvider"></param>
        /// <param name="zipFileName"></param>
        /// <param name="directory"></param>
        /// <returns>Backup of a directory.</returns>
        private static DirectoryBackup BackupDirectory(IFilesStorageProviderV30 filesStorageProvider, string zipFileName, string directory)
        {
            DirectoryBackup directoryBackup = new DirectoryBackup();
            string[] files = filesStorageProvider.ListFiles(directory);

            List<FileBackup> filesBackup = new List<FileBackup>(files.Length);
            if (directory != null)
            {
                Directory.CreateDirectory(Path.Combine(zipFileName.Trim('/').Trim('\\'), directory.Trim('/').Trim('\\')));
            }

            foreach (string file in files)
            {
                FileDetails fileDetails = filesStorageProvider.GetFileDetails(file);
                filesBackup.Add(new FileBackup()
                {
                    Name = file,
                    Size = fileDetails.Size,
                    LastModified = fileDetails.LastModified
                });
                FileStream tempFile = File.Create(Path.Combine(zipFileName.Trim('/').Trim('\\'), file.Trim('/').Trim('\\')));
                using (MemoryStream stream = new MemoryStream())
                {
                    filesStorageProvider.RetrieveFile(file, stream, false);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    tempFile.Write(buffer, 0, buffer.Length);
                    tempFile.Close();
                }
            }
            directoryBackup.Name = directory;
            directoryBackup.Files = filesBackup;

            string[] directories = filesStorageProvider.ListDirectories(directory);
            List<DirectoryBackup> subdirectoriesBackup = new List<DirectoryBackup>(directories.Length);
            foreach (string d in directories)
            {
                subdirectoriesBackup.Add(BackupDirectory(filesStorageProvider, zipFileName, d));
            }
            directoryBackup.SubDirectories = subdirectoriesBackup;

            return directoryBackup;
        }

        #endregion

        #region Restoring Pages and Attachments

        /// <summary>
        /// Restores pages and attachments.
        /// </summary>
        /// <param name="backupFileAddress"></param>
        /// <param name="pagesStorageProvider">Destination pages storage provider.</param>
        /// <param name="filesStorageProvider">Destination files storage provider</param>
        /// <returns><c>true</c>if restore succeeded.</returns>
        public static bool RestorePagesStorageProvider(string backupFileAddress, IPagesStorageProviderV30 pagesStorageProvider, IFilesStorageProviderV30 filesStorageProvider)
        {
            string tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(backupFileAddress, Path.Combine(tempPath, "Backup.zip"));
            }
            catch (WebException ex)
            {
                throw ex;
            }

            //VersionFile versionFile;
            using (ZipFile pagesBackupZipFile = ZipFile.Read(Path.Combine(tempPath, "Backup.zip")))
            {
                ZipEntry versionEntry = (from e in pagesBackupZipFile
                                         where e.FileName == "Version.json"
                                         select e).FirstOrDefault();
                //versionFile = null;

                // Restoring namespaces.
                ZipEntry namespacesEntry = (from e in pagesBackupZipFile
                                            where e.FileName == "Namespaces.json"
                                            select e).FirstOrDefault();
                if (namespacesEntry != null)
                {
                    DeserializeNamespacesBackupStep1(Encoding.Unicode.GetString(ExtractEntry(namespacesEntry)), pagesStorageProvider);
                }

                // Restore pages.
                List<ZipEntry> pageEntries = (from e in pagesBackupZipFile
                                              where e.FileName != "Namespaces.json" &&
                                                    e.FileName != "ContentTemplates.json" &&
                                                    e.FileName != "Snippets.json" &&
                                                    e.FileName != "Version.json" &&
                                                    !e.FileName.Contains("_attachments")
                                              select e).ToList();
                foreach (ZipEntry pageEntry in pageEntries)
                {
                    DeserializePageBackup(Encoding.Unicode.GetString(ExtractEntry(pageEntry)), pagesStorageProvider);
                }

                // Restore content templates
                ZipEntry contentTemplatesEntry = (from e in pagesBackupZipFile
                                                  where e.FileName == "ContentTemplates.json"
                                                  select e).FirstOrDefault();
                if (contentTemplatesEntry != null)
                {
                    DeserializeContentTemplatesBackup(Encoding.Unicode.GetString(ExtractEntry(contentTemplatesEntry)), pagesStorageProvider);
                }

                // Restore snippets.
                ZipEntry snippetsEntry = (from e in pagesBackupZipFile
                                          where e.FileName == "Snippets.json"
                                          select e).FirstOrDefault();
                if (snippetsEntry != null)
                {
                    DeserializeSnippetsBackup(Encoding.Unicode.GetString(ExtractEntry(snippetsEntry)), pagesStorageProvider);
                }

                if (namespacesEntry != null)
                {
                    DeserializeNamepsacesBackupStep2(Encoding.Unicode.GetString(ExtractEntry(namespacesEntry)), pagesStorageProvider);
                }

                // Restore pages attachments.
                ZipEntry attachmentsEntry = (from e in pagesBackupZipFile
                                             where e.FileName.EndsWith("Attachments.json")
                                             select e).FirstOrDefault();
                if (attachmentsEntry != null)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        attachmentsEntry.Extract(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, (int)stream.Length);
                        DeserializeAttachmentsbackup(pagesBackupZipFile, Encoding.Unicode.GetString(buffer),
                            filesStorageProvider, pagesStorageProvider);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Restore pages from a json string.
        /// </summary>
        /// <param name="json">Json-backup of a page.</param>
        /// <param name="pagesStorageProvider">Destination pages storage provider.</param>
        private static void DeserializePageBackup(string json, IPagesStorageProviderV30 pagesStorageProvider)
        {
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = javaScriptSerializer.MaxJsonLength * 10;

            PageBackup pageBackup = javaScriptSerializer.Deserialize<PageBackup>(json);
            PageInfo page = new PageInfo(pageBackup.FullName, pagesStorageProvider, pageBackup.CreationDateTime);//????

            // CurrnetVersion.
            if (pagesStorageProvider.GetPage(pageBackup.FullName) == null)
            {
                pagesStorageProvider.AddPage(pageBackup.NameSpace,
                    pageBackup.NameSpace != string.Empty
                        ? NameTools.GetLocalName(pageBackup.FullName)
                        : pageBackup.FullName,
                    pageBackup.CreationDateTime);
            }
            pagesStorageProvider.ModifyPage(page, pageBackup.Title, pageBackup.User, DateTime.Now, pageBackup.Comment,
                pageBackup.Content, pageBackup.Keywords, pageBackup.Description, SaveMode.Backup);
            pagesStorageProvider.RebindPage(page, pageBackup.Categories);
        }

        /// <summary>
        /// Restore content templates from a json string.
        /// </summary>
        /// <param name="json">Json-backup of a content template.</param>
        /// <param name="pagesStorageProvider">Destination pages storage provider.</param>
        private static void DeserializeContentTemplatesBackup(string json, IPagesStorageProviderV30 pagesStorageProvider)
        {
            JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
            javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

            List<ContentTemplateBackup> contentTemplatesBackup = javascriptSerializer.Deserialize<List<ContentTemplateBackup>>(json);
            var contTemplates = pagesStorageProvider.GetContentTemplates();
            foreach (ContentTemplateBackup contentTemplate in contentTemplatesBackup)
            {
                ContentTemplate ct = new ContentTemplate(contentTemplate.Name, contentTemplate.Content, pagesStorageProvider);
                if (!contTemplates.ToList().Contains(ct))
                {
                    pagesStorageProvider.AddContentTemplate(contentTemplate.Name, contentTemplate.Content);
                }
            }
        }

        /// <summary>
        /// Restores snippets from a json-string..
        /// </summary>
        /// <param name="json">Json-backup of a snippet.</param>
        /// <param name="pagesStorageProvider">Destination pages storage provider.</param>
        private static void DeserializeSnippetsBackup(string json, IPagesStorageProviderV30 pagesStorageProvider)
        {
            JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
            javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

            List<SnippetBackup> snippetsBackup = javascriptSerializer.Deserialize<List<SnippetBackup>>(json);
            foreach (SnippetBackup snippet in snippetsBackup)
            {
                Snippet sn = new Snippet(snippet.Name, snippet.Content, pagesStorageProvider);
                if (!pagesStorageProvider.GetSnippets().ToList().Contains(sn))
                {
                    pagesStorageProvider.AddSnippet(snippet.Name, snippet.Content);
                }
            }
        }

        /// <summary>
        /// Restores namespaces - 1 step.
        /// </summary>
        /// <param name="json">Json-backup of namespaces.</param>
        /// <param name="pagesStorageProvider">Destination pages storage provider.</param>
        private static void DeserializeNamespacesBackupStep1(string json, IPagesStorageProviderV30 pagesStorageProvider)
        {
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = javaScriptSerializer.MaxJsonLength * 10;

            List<NamespaceBackup> namespacesBackup = javaScriptSerializer.Deserialize<List<NamespaceBackup>>(json);

            // Restores namespaces.
            foreach (NamespaceBackup namespaceBackup in namespacesBackup)
            {
                if (namespaceBackup.Name != "")
                {
                    if (pagesStorageProvider.GetNamespace(namespaceBackup.Name) == null)
                    {
                        pagesStorageProvider.AddNamespace(namespaceBackup.Name);
                    }
                }

                // REstores namespaces categories.
                foreach (CategoryBackup category in namespaceBackup.Categories)
                {
                    if (pagesStorageProvider.GetCategory(category.FullName) == null)
                    {
                        pagesStorageProvider.AddCategory(namespaceBackup.Name, NameTools.GetLocalName(category.FullName));
                    }
                }
            }

        }

        /// <summary>
        /// Restores namespaces - 2 xtep.
        /// </summary>
        /// <param name="json">Json-backup of namespaces.</param>
        /// <param name="pagesStorageProvider">Destination pages storage provider.</param>
        private static void DeserializeNamepsacesBackupStep2(string json, IPagesStorageProviderV30 pagesStorageProvider)
        {
            JavaScriptSerializer javascriptSerializer = new JavaScriptSerializer();
            javascriptSerializer.MaxJsonLength = javascriptSerializer.MaxJsonLength * 10;

            List<NamespaceBackup> namespacesBackup = javascriptSerializer.Deserialize<List<NamespaceBackup>>(json);

            // Rebind default pages of namespaces.
            foreach (NamespaceBackup namespaceBackup in namespacesBackup)
            {
                if (namespaceBackup.Name != "")
                {
                    PageInfo dpInfo = new PageInfo(namespaceBackup.DefaultPageFullName, pagesStorageProvider, DateTime.Now);
                    pagesStorageProvider.SetNamespaceDefaultPage(
                        new NamespaceInfo(namespaceBackup.Name, pagesStorageProvider,
                            dpInfo), dpInfo);
                }
            }
        }

        /// <summary>
        /// Restores attachments from zip and json files.
        /// </summary>
        /// <param name="filesBackup">Zip containing attachments backups.</param>
        /// <param name="json">Json-string for attachments.</param>
        /// <param name="filesStorageProvider"></param>
        /// <param name="pagesStorageProvider"></param>
        private static void DeserializeAttachmentsbackup(ZipFile filesBackup, string json,
    IFilesStorageProviderV30 filesStorageProvider, IPagesStorageProviderV30 pagesStorageProvider)
        {
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = javaScriptSerializer.MaxJsonLength * 10;

            List<AttachmentBackup> attachmentsBackup = javaScriptSerializer.Deserialize<List<AttachmentBackup>>(json);

            foreach (AttachmentBackup attachment in attachmentsBackup)
            {
                ZipEntry entry =
                    filesBackup.FirstOrDefault(
                        e => e.FileName == "__attachments/" + attachment.PageFullName + "/" + attachment.Name);
                if (entry != null)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        entry.Extract(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        PageInfo page = pagesStorageProvider.GetPage(attachment.PageFullName);
                        filesStorageProvider.StorePageAttachment(page, attachment.Name, stream, true);
                    }
                }
            }
        }

        #endregion

        #region Restoring Files

        /// <summary>
        /// Restore files storage from a zip file excluding pages' attachments.
        /// </summary>
        /// <param name="backupFileAddress">URL of the zip backup file.</param>
        /// <param name="filesStorageProvider">The file provider storing files.</param>
        /// <returns></returns>
        public static bool RestoreFilesStorageProvider(string backupFileAddress, IFilesStorageProviderV30 filesStorageProvider)
        {
            string tempPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(backupFileAddress, Path.Combine(tempPath, "Backup.zip"));
            }
            catch (WebException ex)
            {
                throw ex;
            }

            using (ZipFile filesBackupZipFile = ZipFile.Read(backupFileAddress))
            {
                ZipEntry filesEntry = (from e in filesBackupZipFile
                                       where e.FileName == "Files.json"
                                       select e).FirstOrDefault();
                if (filesEntry != null)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        filesEntry.Extract(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, (int)stream.Length);
                        DeserializeFilesBackup(filesBackupZipFile, Encoding.Unicode.GetString(buffer), filesStorageProvider);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Exctract data from a file in a zip archive.
        /// </summary>
        /// <param name="zipEntry">File from a zip archive.</param>
        /// <returns></returns>
        private static byte[] ExtractEntry(ZipEntry zipEntry)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                zipEntry.Extract(stream);
                stream.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                return buffer;
            }
        }

        private static void DeserializeFilesBackup(ZipFile filesZipFile, string json,
            IFilesStorageProviderV30 filesStorageProvider)
        {
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = javaScriptSerializer.MaxJsonLength * 10;

            DirectoryBackup directoryBackup = javaScriptSerializer.Deserialize<DirectoryBackup>(json);

            string path = "";
            RestoreDirectory(filesStorageProvider, filesZipFile, directoryBackup, path);

        }

        private static void RestoreDirectory(IFilesStorageProviderV30 filesStorageProvider, ZipFile filesBackupZipFile,
            DirectoryBackup directoryBackup, string path)
        {
            if (!string.IsNullOrEmpty(directoryBackup.Name))
            {
                if (path == "")
                {
                    filesStorageProvider.CreateDirectory("/", directoryBackup.Name.Trim('/'));
                }
                else
                {
                    string auxName = directoryBackup.Name.TrimEnd('/');
                    int start = auxName.LastIndexOf('/');
                    filesStorageProvider.CreateDirectory(path, directoryBackup.Name.Substring(start, directoryBackup.Name.Length - start).TrimStart('/'));
                }

                if (directoryBackup.SubDirectories.ToList().Count == 0)
                {
                    string auxPath = directoryBackup.Name.TrimEnd('/');
                    path = auxPath.Substring(0, auxPath.LastIndexOf('/'));
                }
                else
                {
                    path = directoryBackup.Name;
                }

            }

            foreach (FileBackup file in directoryBackup.Files)
            {
                ZipEntry entry = filesBackupZipFile.FirstOrDefault(e => e.FileName == file.Name.TrimStart('/'));
                if (entry != null)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        entry.Extract(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        filesStorageProvider.StoreFile(file.Name, stream, true);
                    }
                }
            }

            foreach (DirectoryBackup subDirectory in directoryBackup.SubDirectories)
            {
                RestoreDirectory(filesStorageProvider, filesBackupZipFile, subDirectory, path);
            }
        }

        #endregion
    }

    #region Backup Internal Classes

    internal class SettingsBackup
    {
        public Dictionary<string, string> Settings { get; set; }
        public List<string> PluginsFileNames { get; set; }
        public Dictionary<string, bool> PluginsStatus { get; set; }
        public Dictionary<string, string> PluginsConfiguration { get; set; }
        public List<MetaData> Metadata { get; set; }
        public List<RecentChange> RecentChanges { get; set; }
        public Dictionary<string, string[]> OutgoingLinks { get; set; }
        public List<AclEntryBackup> AclEntries { get; set; }
    }

    internal class AclEntryBackup
    {
        public Value Value { get; set; }
        public string Subject { get; set; }
        public string Resource { get; set; }
        public string Action { get; set; }
    }

    internal class MetaData
    {
        public MetaDataItem Item { get; set; }
        public string Tag { get; set; }
        public string Content { get; set; }
    }

    internal class GlobalSettingsBackup
    {
        public Dictionary<string, string> Settings { get; set; }
        public List<string> pluginsFileNames { get; set; }
    }

    internal class PageBackup
    {
        public String NameSpace { get; set; }
        public String FullName { get; set; }
        public DateTime CreationDateTime { get; set; }
        public DateTime LastModified { get; set; }
        public string Content { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
        public string Title { get; set; }
        public string User { get; set; }
        public string[] LinkedPages { get; set; }
        public List<PageRevisionBackup> Revisions { get; set; }
        public PageRevisionBackup Draft { get; set; }
        public List<MessageBackup> Messages { get; set; }
        public string[] Categories { get; set; }
    }

    internal class PageRevisionBackup
    {
        public string Content { get; set; }
        public string Comment { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
        public string Title { get; set; }
        public string User { get; set; }
        public DateTime LastModified { get; set; }
        public int Revision { get; set; }
    }

    internal class NamespaceBackup
    {
        public string Name { get; set; }
        public string DefaultPageFullName { get; set; }
        public List<CategoryBackup> Categories { get; set; }
        public List<NavigationPathBackup> NavigationPaths { get; set; }
    }

    internal class CategoryBackup
    {
        public string FullName { get; set; }
        public string[] Pages { get; set; }
    }

    internal class ContentTemplateBackup
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }

    internal class MessageBackup
    {
        public List<MessageBackup> Replies { get; set; }
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime DateTime { get; set; }
        public string Username { get; set; }
    }

    internal class NavigationPathBackup
    {
        public string FullName { get; set; }
        public string[] Pages { get; set; }
    }

    internal class SnippetBackup
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }

    internal class UserBackup
    {
        public string Username { get; set; }
        public bool Active { get; set; }
        public DateTime DateTime { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string[] Groups { get; set; }
        public IDictionary<string, string> UserData { get; set; }
    }

    internal class UserGroupBackup
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    internal class DirectoryBackup
    {
        public List<FileBackup> Files { get; set; }
        public List<DirectoryBackup> SubDirectories { get; set; }
        public string Name { get; set; }
    }

    internal class FileBackup
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string DirectoryName { get; set; }
    }

    internal class VersionFile
    {
        public string BackupRestoreVersion { get; set; }
        public string WikiVersion { get; set; }
        public string BackupName { get; set; }
    }

    internal class AttachmentBackup
    {
        public string Name { get; set; }
        public string PageFullName { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }
    #endregion
}
