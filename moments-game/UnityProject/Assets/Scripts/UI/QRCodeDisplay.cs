using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates and displays a QR code on the TV attract screen.
/// Uses ZXing.Net (via Unity package or DLL) to encode the join URL.
/// Attach to the QR code RawImage on the Attract/Lobby canvas.
/// </summary>
public class QRCodeDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage qrImage;
    [SerializeField] private TMPro.TextMeshProUGUI roomCodeText;
    [SerializeField] private TMPro.TextMeshProUGUI joinUrlText;

    [Header("Config")]
    [SerializeField] private int qrTextureSize = 512;
    [SerializeField] private Color foregroundColor = Color.black;
    [SerializeField] private Color backgroundColor = Color.white;

    private void Start()
    {
        RefreshQR();

        // Re-generate if session restarts
        if (SessionStateManager.Instance != null)
            SessionStateManager.Instance.OnStateChanged += OnSessionStateChanged;
    }

    private void OnSessionStateChanged(SessionStateManager.LobbyState state)
    {
        if (state == SessionStateManager.LobbyState.Attract ||
            state == SessionStateManager.LobbyState.WaitingForPlayers)
            RefreshQR();
    }

    public void RefreshQR()
    {
        var token = SessionStateManager.Instance?.RoomToken ?? "DEMO";
        var localIP = PhoneControllerServer.Instance != null
            ? FindObjectOfType<PhoneControllerServer>()?.LocalIP ?? "localhost"
            : "localhost";

        var joinUrl = $"http://{localIP}:8080/?room={token}";

        // Generate QR texture using ZXing
        var texture = GenerateQRTexture(joinUrl);
        if (qrImage != null) qrImage.texture = texture;
        if (roomCodeText != null) roomCodeText.text = token;
        if (joinUrlText != null) joinUrlText.text = $"moments.local or scan QR";

        Debug.Log($"[QR] Join URL: {joinUrl}");
    }

    private Texture2D GenerateQRTexture(string content)
    {
        // ZXing.Net encoding
        try
        {
#if ZXING_AVAILABLE
            var writer = new ZXing.BarcodeWriterTexture
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Options = new ZXing.QrCode.QrCodeEncodingOptions
                {
                    Width = qrTextureSize,
                    Height = qrTextureSize,
                    Margin = 2,
                    ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
                    CharacterSet = "UTF-8"
                }
            };

            var texture = new Texture2D(qrTextureSize, qrTextureSize, TextureFormat.RGBA32, false);
            writer.Write(content, texture);

            // Apply custom colors
            var pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = pixels[i].a > 128
                    ? ColorToColor32(foregroundColor)
                    : ColorToColor32(backgroundColor);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
#else
            // Fallback: placeholder checkerboard texture if ZXing not installed
            return GeneratePlaceholderTexture(content);
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QR] Generation failed: {e.Message}. Using placeholder.");
            return GeneratePlaceholderTexture(content);
        }
    }

    private Texture2D GeneratePlaceholderTexture(string content)
    {
        var tex = new Texture2D(qrTextureSize, qrTextureSize, TextureFormat.RGBA32, false);
        var pixels = new Color32[qrTextureSize * qrTextureSize];
        for (int y = 0; y < qrTextureSize; y++)
            for (int x = 0; x < qrTextureSize; x++)
                pixels[y * qrTextureSize + x] = ((x / 16 + y / 16) % 2 == 0)
                    ? new Color32(0, 0, 0, 255)
                    : new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();

        Debug.Log($"[QR] Placeholder QR — install ZXing.Net for real QR. URL: {content}");
        return tex;
    }

    private Color32 ColorToColor32(Color c)
        => new((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(c.a * 255));

    private void OnDestroy()
    {
        if (SessionStateManager.Instance != null)
            SessionStateManager.Instance.OnStateChanged -= OnSessionStateChanged;
    }
}
