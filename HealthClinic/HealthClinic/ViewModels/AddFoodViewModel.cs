﻿using System;
using System.IO;
using System.Windows.Input;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace HealthClinic
{
    public class AddFoodViewModel : BaseViewModel
    {
        #region Fields
        ICommand _takePhotoCommand, _uploadButtonCommand;
        ImageSource _photoImageSource;
        bool _isPhotoUploading;
        byte[] _photoBlob;
        #endregion

        #region Events
        public event EventHandler UploadPhotoCompleted;
        public event EventHandler<string> UploadPhotoFailed;
        #endregion

        #region Properties
        public ICommand TakePhotoCommand => _takePhotoCommand ??
            (_takePhotoCommand = new Command(async () => await ExecuteTakePhotoCommand()));

        public ICommand UploadButtonCommand => _uploadButtonCommand ??
            (_uploadButtonCommand = new Command(async () => await ExecuteUploadButtonCommand()));

        public bool IsPhotoUploading
        {
            get => _isPhotoUploading;
            set => SetProperty(ref _isPhotoUploading, value);
        }

        public ImageSource PhotoImageSource
        {
            get => _photoImageSource;
            set => SetProperty(ref _photoImageSource, value);
        }
        #endregion

        #region Methods
        async Task ExecuteTakePhotoCommand()
        {
            var mediaFile = await MediaService.GetMediaFileFromCamera().ConfigureAwait(false);

            if (mediaFile == null)
                return;

            _photoBlob = ConvertStreamToByteArrary(mediaFile.GetStream());
            PhotoImageSource = ImageSource.FromStream(() => new MemoryStream(_photoBlob));
        }

        async Task ExecuteUploadButtonCommand()
        {
            if (IsPhotoUploading)
                return;

            if (_photoBlob == null)
            {
                OnUploadPhotoFailed("Take Photo First");
                return;
            }

            IsPhotoUploading = true;

            try
            {
                var postPhotoBlobResponse = await FoodListAPIService.PostFoodPhoto(_photoBlob).ConfigureAwait(false);

                if (postPhotoBlobResponse == null || postPhotoBlobResponse?.IsSuccessStatusCode == false)
                {
                    OnUploadPhotoFailed($"Status Code: {postPhotoBlobResponse?.ReasonPhrase}");
                    return;
                }

                OnSavePhotoCompleted();
            }
            catch (Exception e)
            {
                OnUploadPhotoFailed(e.Message);
            }
            finally
            {
                IsPhotoUploading = false;
            }
        }

        byte[] ConvertStreamToByteArrary(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        void OnUploadPhotoFailed(string errorMessage) => UploadPhotoFailed?.Invoke(this, errorMessage);
        void OnSavePhotoCompleted() => UploadPhotoCompleted?.Invoke(this, EventArgs.Empty);
        #endregion
    }
}