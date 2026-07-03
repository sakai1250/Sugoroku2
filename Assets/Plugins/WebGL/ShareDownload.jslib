mergeInto(LibraryManager.library, {
  Sugoroku_DownloadBase64File: function (base64Ptr, filenamePtr, mimePtr) {
    var base64 = UTF8ToString(base64Ptr);
    var filename = UTF8ToString(filenamePtr);
    var mime = UTF8ToString(mimePtr);

    try {
      var byteChars = atob(base64);
      var byteNumbers = new Array(byteChars.length);
      for (var i = 0; i < byteChars.length; i++) {
        byteNumbers[i] = byteChars.charCodeAt(i);
      }
      var byteArray = new Uint8Array(byteNumbers);
      var blob = new Blob([byteArray], { type: mime });
      var url = URL.createObjectURL(blob);

      var link = document.createElement('a');
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    } catch (e) {
      console.error('Sugoroku_DownloadBase64File failed:', e);
    }
  },

  Sugoroku_OpenUrl: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    window.open(url, '_blank');
  }
});
