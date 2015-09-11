(function() {

    var AppModule = {
        CurrentViewModeCookieKey: "CurrentViewMode",
        defaultDocumentName: "",
        documentChanged: false,
        savingInProgress: false,
        loadingPanelTimer: 0,
        deferredAction: null,
        actionDelay: undefined,

        onFileManagerToolbarUpdating: function(s, e) {
        },

        onFolderItemLinkClick: function(evt) {
            AppModule.collapseLeftPanelAndExecuteAction(function() {
                AppModule.openCurrentFolder();
            });
        },

        onFileItemLinkClick: function(evt) {
            AppModule.collapseLeftPanel();
        },

        openCurrentFolder: function() {
            var viewMode = ASPxClientUtils.GetCookie(AppModule.CurrentViewModeCookieKey);
            if(viewMode === "Details")
                window.setTimeout(AppModule.openCurrentFolderInternal, 0);
            else
                AppModule.openCurrentFolderInternal();
        },

        openCurrentFolderInternal: function() {
            FileManager.ExecuteCommand(ASPxClientFileManagerCommandConsts.Open);
        },
            
        onFilterMenuItemClick: function(s, e) {
            var createItemActionVisibility = e.item.name === "All";
            ActionMenu.GetItemByName("New").SetVisible(createItemActionVisibility);
            ActionMenu.GetItemByName("Upload").SetVisible(createItemActionVisibility);
            AppModule.collapseLeftPanelAndExecuteAction(function() {
                AppModule.reloadContentPane();
            });
        },

        reloadContentPane: function() {
            CallbackPanel.PerformCallback();
        },

        collapseLeftPanelAndExecuteAction: function(action, delay) {
            if(LeftPanel.IsExpandable() && LeftPanel.IsExpanded()) {
                AppModule.deferredAction = action;
                AppModule.actionDelay = delay;
                AppModule.collapseLeftPanel();
            }
            else
                action();
        },

        collapseLeftPanel: function() {
            LeftPanel.Collapse();
        },

        onLeftPanelCollapsed: function(s, e) {
            if(AppModule.deferredAction != null) {
                var action = AppModule.deferredAction;
                AppModule.deferredAction = null;
                if(AppModule.actionDelay === undefined)
                    action();
                else
                    window.setTimeout(action, AppModule.actionDelay);
            }
        },

        onItemLinkMouseDown: function(s, e) {
        },

        updateActionMenu: function(items) {
            var visibility = items.length > 0;
            var extendedEdit = FilterMenu.GetSelectedItem().name === "All";
            ActionMenu.GetItemByName("Rename").SetVisible(items.length == 1 && extendedEdit);
            ActionMenu.GetItemByName("Move").SetVisible(visibility && extendedEdit);
            ActionMenu.GetItemByName("Copy").SetVisible(visibility && extendedEdit);
            ActionMenu.GetItemByName("Delete").SetVisible(visibility && extendedEdit);
            
            var downloadVisible = visibility;
            for(var i = 0; i < items.length; i++) {
                var item = items[i];
                if(item instanceof ASPxClientFileManagerFolder) {
                    downloadVisible = false;
                    break;
                }
            }
            ActionMenu.GetItemByName("Download").SetVisible(downloadVisible);
        },

        adjustActionMenu: function() {
        	 ActionMenu.AdjustControl();
        },
        
        onActionMenuItemClick: function(s, e) {
            var commandName = e.item.name;
            if(commandName === "New")
                return;

            var docType = "";
            switch(commandName) {
                case "RichTextDocument":
                    docType = "richtext";
                    break;
                case "WorksheetDocument":
                    docType = "worksheet";
                    break;
            }
            if(docType) {
                AppModule.collapseLeftPanel();
                AppModule.openNewDocument(FileManager.GetCurrentFolderPath(), docType);
            }
            else {
                var delay = commandName === "Folder" ? 1000 : undefined;
                AppModule.collapseLeftPanelAndExecuteAction(function() {
                    AppModule.executeFileManagerCommandByName(commandName);
                },  delay);
            }
        },

        executeFileManagerCommandByName: function(commandName) {
            switch(commandName) {
                case "Folder":
                    FileManager.ExecuteCommand(ASPxClientFileManagerCommandConsts.Create);
                    break;
                case "Refresh":
                    FileManager.Refresh();
                    break;
                case "Delete":
                    FileManager.ExecuteCommand(ASPxClientFileManagerCommandConsts.Delete);
                    break;
                case "Rename":
                    FileManager.ExecuteCommand(ASPxClientFileManagerCommandConsts.Rename);
                    break;
                case "Move":
                    FileManager.ExecuteCommand(ASPxClientFileManagerCommandConsts.Move);
                    break;
                case "Download":
                    FileManager.ExecuteCommand(ASPxClientFileManagerCommandConsts.Download);
                    break;
                case "Copy":
                    FileManager.ExecuteCommand(ASPxClientFileManagerCommandConsts.Copy);
                    break;
            }
        },

        onActionMenuInit: function () {
        		AppModule.adjustActionMenu();
            ASPxClientUtils.AttachEventToElement(window, "resize", function(e) {
                AppModule.adjustActionMenu();
            });
        },

        openNewDocument: function (folderPath, docType) {
			 //todo
            var url = window.location.href.substr(0, window.location.href.lastIndexOf("/") + 1);
            url += "Document.aspx?path=" + window.encodeURIComponent(folderPath);
            url += "&type=" + docType;
            window.open(url, "_blank");
        },

        onDocumentEditorInit: function(s, e) {
            ASPxClientUtils.AttachEventToElement(window, "beforeunload", function(e) {
                return AppModule.onWindowBeforeUnload(e);
            });
            ASPxClientUtils.AttachEventToElement(window, "resize", function(e) {
                return AppModule.onWindowResize(s);
            });

            AppModule.onWindowResize(s);
        },
        
        onDocumentEditorDocumentChanged: function(s, e) {
            if(!AppModule.documentChanged) {
                StatusMarkLabel.SetText("Not saved");
                AppModule.documentChanged = true;
            }
        },

        onDocumentEditorBeginSynchronization: function(s, e) {
            if(AppModule.documentChanged) {
                AppModule.savingInProgress = true;
                AppModule.documentChanged = false;
                StatusMarkLabel.SetText("Saving...");
            }
        },

        onDocumentEditorEndSynchronization: function(s, e) {
            if(AppModule.savingInProgress) {
                AppModule.savingInProgress = false;
                StatusMarkLabel.SetText("Saved");
            }
        },

        onDocumentRibbonCommandExecuted: function(commandName, docType) {
            if(commandName == "SaveAsButton") {
                if(window.SaveAsDocumentPopupControl) {
                    DocumentNameTextBox.SetText(AppModule.defaultDocumentName);
                    SaveAsDocumentPopupControl.Show();
                }
                else
                    DialogCallbackPanel.PerformCallback();
            }
            else if(commandName == "NewButton") {
                var existingDocType = getQueryParameterByName("type");
                var folderPath = getQueryParameterByName("path");
                if(!existingDocType) {
                    var lastSeparatorIndex = folderPath.lastIndexOf("\\");
                    if(lastSeparatorIndex > -1)
                        folderPath = folderPath.substr(0, lastSeparatorIndex);
                }
                AppModule.openNewDocument(folderPath, docType);
            }
        },

        onSaveAsDialogInit: function(s, e) {
            AppModule.defaultDocumentName = DocumentNameTextBox.GetText();
        },

        onSaveAsDialogSaveButtonClick: function(s, e) {
            SaveAsDocumentPopupControl.Hide();
            var newDocumentName = DocumentNameTextBox.GetText();
            DocumentEditorCallback.PerformCallback(newDocumentName);
        },

        onSaveAsDialogCancelButtonClick: function(s, e) {
            SaveAsDocumentPopupControl.Hide();
        },

        onDocumentEditorCallbackComplete: function(s, e) {
            if(!e.result) return;
            
            var message = "";
            if(e.result === "exists")
                message = "Document '" + e.parameter + "' already exists.";
            else
                message = "This document format is not supported.";
            
            window.alert(message);
        },

        onUserMenuItemClick: function(s, e) {
        	 e.processOnServer = (e.item.name === "SignOut");
        },

        onViewModeMenuItemClick: function(s, e) {
            ASPxClientUtils.SetCookie(AppModule.CurrentViewModeCookieKey, e.item.name);
            CallbackPanel.PerformCallback();
        },

        onWindowBeforeUnload: function(e) {
            if(AppModule.documentChanged || AppModule.savingInProgress) {
                var confirmMessage = "Are you sure you want to perform the action? All unsaved document data will be lost.";
                e.returnValue = confirmMessage;
                return confirmMessage;
            }
        },

        onWindowResize: function(documentEditor) {
            var windowHeight = window.innerHeight || ASPxClientUtils.GetDocumentClientHeight();
            var editorHeight = windowHeight - documentEditor.GetMainElement().offsetTop;

            documentEditor.SetHeight(editorHeight);
        },

    	  //added
        adjustFileManager: function () {
        	 AppModule.onWindowResize(FileManager);
        	 FileManager.AdjustControl();
        },

        onControlInit: function (s, e) {
        	 AppModule.adjustFileManager();
        	 ASPxClientUtils.AttachEventToElement(window, "resize", function (e) {
        	 	 AppModule.adjustFileManager();
        	 });
        },

    	  //changed
        onControlBeginCallback: function(s, e) {
        	 AppModule.showLoadingPanel();
        	 e.customArgs["filtered"] = FilterMenu.GetSelectedItem().name;        	 
        },
        //changed
        onControlEndCallback: function(s, e) {
        	 AppModule.hideLoadingPanel();
        	 AppModule.adjustFileManager();
        },

        onUserPasswordInit: function(s, e) {
            s.SetValue("Password");
        },

        showLoadingPanel: function() {
            window.loadingPanelTimer = window.setTimeout(function() {
                LoadingPanel.Show();
            }, 300);
        },

        hideLoadingPanel: function() {
            window.clearTimeout(window.loadingPanelTimer);
            LoadingPanel.Hide();
        },

        onError: function() {
            if(LoadingPanel && LoadingPanel.GetMainElement())
                App.hideLoadingPanel();
        }
    };

    var getQueryParameterByName = function(name) {
        name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
        var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
            results = regex.exec(location.search);
        return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
    };

    window.onerror = function() { 
        AppModule.onError();
    };

    window.App = AppModule;
}());
