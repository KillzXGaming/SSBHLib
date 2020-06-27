﻿using CrossMod.Nodes;
using CrossMod.Rendering;
using CrossMod.Rendering.GlTools;
using CrossMod.Tools;
using CrossModGui.ViewModels;
using CrossModGui.Views;
using System;
using System.Windows;

namespace CrossModGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel viewModel;

        private readonly RenderSettingsWindowViewModel renderSettingsViewModel;
        private readonly CameraSettingsWindowViewModel cameraSettingsViewModel;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.viewModel.Renderer = new ViewportRenderer(glViewport);
            DataContext = viewModel;

            // Link view models to models.
            renderSettingsViewModel = RenderSettings.Instance;
            renderSettingsViewModel.PropertyChanged += (s, e) => RenderSettings.Instance.SetValues(renderSettingsViewModel);

            cameraSettingsViewModel = this.viewModel.Renderer.Camera;
            cameraSettingsViewModel.PropertyChanged += CameraSettingsViewModel_PropertyChanged;

            this.viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Ensure animations update the viewport.
            if (e.PropertyName == nameof(MainWindowViewModel.IsPlayingAnimation))
            {
                if (viewModel.IsPlayingAnimation)
                    glViewport.RestartRendering();
                else
                    glViewport.PauseRendering();
            }
            if (e.PropertyName == nameof(MainWindowViewModel.CurrentFrame))
            {
                glViewport.RenderFrame();
            }
        }

        private void CameraSettingsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // TODO: Do this with less code using reactiveui?
            switch (e.PropertyName)
            {
                case nameof(CameraSettingsWindowViewModel.RotationXDegrees):
                    viewModel.Renderer.Camera.RotationXDegrees = cameraSettingsViewModel.RotationXDegrees;
                    break;
                case nameof(CameraSettingsWindowViewModel.RotationYDegrees):
                    viewModel.Renderer.Camera.RotationYDegrees = cameraSettingsViewModel.RotationYDegrees;
                    break;
                case nameof(CameraSettingsWindowViewModel.PositionX):
                    viewModel.Renderer.Camera.TranslationX = cameraSettingsViewModel.PositionX;
                    break;
                case nameof(CameraSettingsWindowViewModel.PositionY):
                    viewModel.Renderer.Camera.TranslationY = cameraSettingsViewModel.PositionY;
                    break;
                case nameof(CameraSettingsWindowViewModel.PositionZ):
                    viewModel.Renderer.Camera.TranslationZ = cameraSettingsViewModel.PositionZ;
                    break;
            }
        }

        private void GlViewport_OnRenderFrame(object sender, EventArgs e)
        {
            // TODO: Script node.
            viewModel.Renderer.RenderNodes(null);
        }

        private void RenderSettings_Click(object sender, RoutedEventArgs e)
        {
            CreateDisplayEditorWindow(new RenderSettingsWindow(renderSettingsViewModel));
        }

        private void Camera_Click(object sender, RoutedEventArgs e)
        {
            // Make sure the window has current values.
            cameraSettingsViewModel.SetValues(viewModel.Renderer.Camera);
            CreateDisplayEditorWindow(new CameraSettingsWindow(cameraSettingsViewModel));
        }

        private void MaterialEditor_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new MaterialEditorWindowViewModel();

            // TODO: Get actual data from matl and sync to material used for rendering.
            viewModel.MaterialNames.Add("material1");
            viewModel.MaterialNames.Add("material2");
            viewModel.PossibleTextureNames.Add("texture1");
            viewModel.PossibleTextureNames.Add("texture2");
            viewModel.PossibleTextureNames.Add("#replace_cubemap");
            viewModel.BooleanParams.Add(new MaterialEditorWindowViewModel.BooleanParam { Name = "CustomBoolean0", Value = false });
            viewModel.BooleanParams.Add(new MaterialEditorWindowViewModel.BooleanParam { Name = "CustomBoolean1", Value = true });
            viewModel.FloatParams.Add(new MaterialEditorWindowViewModel.FloatParam { Name = "CustomFloat0", Value = 1 });
            viewModel.FloatParams.Add(new MaterialEditorWindowViewModel.FloatParam { Name = "CustomFloat1", Value = 2.5f });
            viewModel.Vec4Params.Add(new MaterialEditorWindowViewModel.Vec4Param { Name = "CustomVector0", Value1 = 1, Value2 = 2, Value3 = 3, Value4 = 4 });
            viewModel.Vec4Params.Add(new MaterialEditorWindowViewModel.Vec4Param { Name = "CustomVector1", Value1 = 5, Value2 = 6, Value3 = 7, Value4 = 8 });
            viewModel.TextureParams.Add(new MaterialEditorWindowViewModel.TextureParam { Name = "Texture0", Value = "texture1" });
            viewModel.TextureParams.Add(new MaterialEditorWindowViewModel.TextureParam { Name = "Texture1", Value = "texture2" });
            CreateDisplayEditorWindow(new MaterialEditorWindow(viewModel));
        }

        private void CreateDisplayEditorWindow(Window window)
        {
            // Start automatic frame updates instead of making the window have to refresh the viewport.
            var wasRendering = glViewport.IsRendering;
            glViewport.RestartRendering();

            window.Show();

            window.Closed += (s, e2) =>
            {
                if (!wasRendering)
                    glViewport.PauseRendering();
            };
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (FileTools.TryOpenFolderDialog(out string folderPath))
            {
                viewModel.PopulateFileTree(folderPath);
            }
        }

        private void ClearWorkspace_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Clear();
            viewModel.Renderer.ClearRenderableNodes();
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!(e.NewValue is FileNode item))
                return;

            // Open all files in the folder when the folder is selected.
            // TODO: This could be moved to the expanded event instead.
            if (item.Parent is DirectoryNode dir)
            {
                dir.OpenFileNodes();
            }

            // Update the current viewport item.
            viewModel.UpdateCurrentRenderableNode(item);
            RenderFrameIfNeeded();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Don't start rendering here to save CPU usage.
            glViewport.OnRenderFrame += GlViewport_OnRenderFrame;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            glViewport.Dispose();
        }

        private void glViewport_Resize(object sender, EventArgs e)
        {
            viewModel.Renderer.Camera.RenderWidth = glViewport.Width;
            viewModel.Renderer.Camera.RenderHeight = glViewport.Height;

            RenderFrameIfNeeded();
        }

        private void glViewport_MouseInteract(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            viewModel.Renderer.UpdateCameraFromMouse();
            cameraSettingsViewModel.SetValues(viewModel.Renderer.Camera);

            RenderFrameIfNeeded();
        }

        private void FrameModel_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Renderer.FrameRenderableModels();
            cameraSettingsViewModel.SetValues(viewModel.Renderer.Camera);

            RenderFrameIfNeeded();
        }

        private void ClearViewport_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Renderer.ClearRenderableNodes();
            RenderFrameIfNeeded();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IsPlayingAnimation = !viewModel.IsPlayingAnimation;
        }

        private void RenderFrameIfNeeded()
        {
            if (!glViewport.IsRendering)
                glViewport.RenderFrame();
        }

        private void MeshListCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Ensure mesh visibility is updated.
            RenderFrameIfNeeded();
        }

        private void ReloadShaders_Click(object sender, RoutedEventArgs e)
        {
            // Force the shaders to be generated again.
            viewModel.Renderer.ReloadShaders();
        }

        private void ReloadScripts_Click(object sender, RoutedEventArgs e)
        {
            // TODO:
        }

        private void BatchRenderModels_Click(object sender, RoutedEventArgs e)
        {
            BatchRendering.RenderModels(viewModel.Renderer);
        }

        private void NextFrame_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CurrentFrame++;
        }

        private void LastFrame_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CurrentFrame = viewModel.TotalFrames;
        }

        private void PreviousFrame_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CurrentFrame--;
        }

        private void FirstFrame_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CurrentFrame = 0f;
        }
    }
}
