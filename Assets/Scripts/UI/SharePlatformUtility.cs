using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Sugoroku.UI
{
    /// <summary>
    /// requirements.md §1.1 プラットフォーム(WebGL / iOS / Android)ごとの
    /// 画像保存・SNSシェア導線を吸収するユーティリティ。
    /// </summary>
    public static class SharePlatformUtility
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void Sugoroku_DownloadBase64File(string base64, string filename, string mime);
#endif

        /// <summary>PNGバイト列を保存/ダウンロードする。戻り値は端末保存時のみ保存先パス、それ以外は null。</summary>
        public static string SaveImage(byte[] pngBytes, string filename)
        {
            if (pngBytes == null || pngBytes.Length == 0) return null;

#if UNITY_WEBGL && !UNITY_EDITOR
            Sugoroku_DownloadBase64File(Convert.ToBase64String(pngBytes), filename, "image/png");
            return null;
#else
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
                System.IO.File.WriteAllBytes(path, pngBytes);
                return path;
            }
            catch (Exception e)
            {
                Debug.LogError($"SharePlatformUtility.SaveImage failed: {e}");
                return null;
            }
#endif
        }

        /// <summary>Xの投稿画面をテキスト付きで開く（画像添付はプラットフォーム制約上、手動添付を促す）。</summary>
        public static void OpenShareIntent(string text)
        {
            string url = "https://twitter.com/intent/tweet?text=" + UnityWebRequestEscape(text);
            Application.OpenURL(url);
        }

        private static string UnityWebRequestEscape(string text) =>
            UnityEngine.Networking.UnityWebRequest.EscapeURL(text ?? "");
    }
}
