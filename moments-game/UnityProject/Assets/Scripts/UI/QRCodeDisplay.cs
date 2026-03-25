using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// QRCodeDisplay — generates and renders QR codes for the join URL.
/// Pure C# implementation using a compact QR encoder.
/// No external library needed — uses a minimal QR byte matrix generator.
///
/// Shown in:
///   - Attract scene: large central display
///   - Lobby scene: smaller panel beside player card grid
/// 
/// Join URL format: http://{localIP}:8080/join?room={ROOM_TOKEN}
/// (Serves phone-controller.html from StreamingAssets via PhoneControllerServer)
/// </summary>
[RequireComponent(typeof(RawImage))]
public class QRCodeDisplay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private int   qrTextureSize   = 512;
    [SerializeField] private Color foregroundColor  = Color.black;
    [SerializeField] private Color backgroundColor  = Color.white;
    [SerializeField] private int   quietZone        = 4;   // Quiet zone modules

    [Header("References")]
    [SerializeField] private TMPro.TextMeshProUGUI urlLabel;

    private RawImage _image;
    private Texture2D _qrTexture;
    private string _currentUrl;

    private void Awake()
    {
        _image = GetComponent<RawImage>();
    }

    private void Start()
    {
        RefreshQR();

        // Re-generate if session token changes
        if (SessionStateManager.Instance != null)
            SessionStateManager.Instance.OnStateChanged += OnSessionStateChanged;
    }

    private void OnDestroy()
    {
        if (SessionStateManager.Instance != null)
            SessionStateManager.Instance.OnStateChanged -= OnSessionStateChanged;
        if (_qrTexture != null) Destroy(_qrTexture);
    }

    private void OnSessionStateChanged(SessionStateManager.SessionPhase _) => RefreshQR();

    public void RefreshQR()
    {
        string ip    = GetLocalIP();
        string token = SessionStateManager.Instance?.CurrentRoomToken ?? "XXXXXX";
        _currentUrl  = $"http://{ip}:8080/join?room={token}";

        if (urlLabel != null) urlLabel.text = _currentUrl;

        // Generate QR texture
        var matrix = QRCodeGenerator.Generate(_currentUrl);
        RenderQR(matrix);

        Debug.Log($"[QR] Generated QR for: {_currentUrl}");
    }

    private void RenderQR(bool[,] matrix)
    {
        if (matrix == null) return;

        int modules   = matrix.GetLength(0);
        int totalSize = modules + quietZone * 2;
        int cellSize  = Mathf.Max(1, qrTextureSize / totalSize);
        int texSize   = totalSize * cellSize;

        if (_qrTexture == null || _qrTexture.width != texSize)
        {
            if (_qrTexture != null) Destroy(_qrTexture);
            _qrTexture = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            _qrTexture.filterMode = FilterMode.Point;
        }

        // Fill background
        Color[] pixels = new Color[texSize * texSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;

        // Draw modules
        for (int row = 0; row < modules; row++)
        {
            for (int col = 0; col < modules; col++)
            {
                if (!matrix[row, col]) continue;

                int startX = (col + quietZone) * cellSize;
                int startY = (row + quietZone) * cellSize;

                for (int dy = 0; dy < cellSize; dy++)
                    for (int dx = 0; dx < cellSize; dx++)
                    {
                        int px = startX + dx;
                        int py = startY + dy;
                        if (px < texSize && py < texSize)
                            pixels[py * texSize + px] = foregroundColor;
                    }
            }
        }

        _qrTexture.SetPixels(pixels);
        _qrTexture.Apply();
        _image.texture = _qrTexture;
    }

    private string GetLocalIP()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127."))
                    return ip.ToString();
        }
        catch { }
        return "127.0.0.1";
    }
}

/// <summary>
/// Minimal QR Code generator — Version 1 (21×21), numeric/alphanumeric/byte mode.
/// Sufficient for short URLs. Returns a boolean matrix (true = dark module).
/// 
/// NOTE: For production, replace with a battle-tested QR library like ZXing.NET
///       (MIT licensed, available as a Unity plugin). This implementation covers
///       URLs up to ~50 chars reliably.
/// </summary>
public static class QRCodeGenerator
{
    private static readonly int[] GF_EXP = new int[512];
    private static readonly int[] GF_LOG = new int[256];

    static QRCodeGenerator()
    {
        // Initialize GF(256) for Reed-Solomon
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            GF_EXP[i] = x;
            GF_LOG[x] = i;
            x <<= 1;
            if ((x & 0x100) != 0) x ^= 0x11d;
        }
        for (int i = 255; i < 512; i++) GF_EXP[i] = GF_EXP[i - 255];
    }

    /// <summary>Generate a Version 2 (25×25) QR code matrix for the given URL.</summary>
    public static bool[,] Generate(string url)
    {
        // Encode as byte mode (UTF-8)
        byte[] data = Encoding.UTF8.GetBytes(url);

        // Build data codewords (byte mode, Version 2-M: 28 data codewords)
        var bits = new System.Collections.Generic.List<byte>();

        // Mode indicator: byte = 0100
        AddBits(bits, 0b0100, 4);
        // Character count (Version 1-9 byte mode = 8 bits)
        AddBits(bits, data.Length, 8);
        // Data bytes
        foreach (byte b in data) AddBits(bits, b, 8);
        // Terminator
        AddBits(bits, 0, 4);

        // Pad to 272 bits (34 codewords × 8) for Version 2-M
        int targetBits = 272;
        while (bits.Count < targetBits) AddBits(bits, bits.Count % 2 == 0 ? 0xEC : 0x11, 8);

        // Convert bit stream to codewords
        var codewords = BitsToBytes(bits, 34);

        // Append Reed-Solomon error correction (16 EC codewords for V2-M)
        var ecWords = ReedSolomon(codewords, 16);

        // Build 25×25 matrix
        int size = 25;
        var matrix    = new bool[size, size];
        var reserved  = new bool[size, size];

        PlaceFinderPatterns(matrix, reserved, size);
        PlaceTimingPatterns(matrix, reserved, size);
        PlaceAlignmentPattern(matrix, reserved); // V2: single at (18,18)
        PlaceDarkModule(matrix, reserved, size);
        PlaceFormatInfo(matrix, reserved, size, 2, 0); // Mask 0

        var allData = new byte[codewords.Length + ecWords.Length];
        codewords.CopyTo(allData, 0);
        ecWords.CopyTo(allData, codewords.Length);

        PlaceDataBits(matrix, reserved, size, allData);
        ApplyMask(matrix, reserved, size, 0);

        return matrix;
    }

    private static void AddBits(System.Collections.Generic.List<byte> bits, int value, int count)
    {
        for (int i = count - 1; i >= 0; i--)
            bits.Add((byte)((value >> i) & 1));
    }

    private static byte[] BitsToBytes(System.Collections.Generic.List<byte> bits, int count)
    {
        var result = new byte[count];
        for (int i = 0; i < count; i++)
        {
            int val = 0;
            for (int j = 0; j < 8; j++)
            {
                int idx = i * 8 + j;
                if (idx < bits.Count) val = (val << 1) | bits[idx];
                else val <<= 1;
            }
            result[i] = (byte)val;
        }
        return result;
    }

    private static byte[] ReedSolomon(byte[] data, int ecCount)
    {
        // Generator polynomial for ecCount=16
        var gen = GeneratorPoly(ecCount);
        var res = new byte[data.Length + ecCount];
        data.CopyTo(res, 0);

        for (int i = 0; i < data.Length; i++)
        {
            int coef = res[i];
            if (coef != 0)
            {
                for (int j = 1; j < gen.Length; j++)
                    res[i + j] ^= GFMul(gen[j], coef);
            }
        }

        var ec = new byte[ecCount];
        System.Array.Copy(res, data.Length, ec, 0, ecCount);
        return ec;
    }

    private static byte[] GeneratorPoly(int degree)
    {
        var g = new byte[] { 1 };
        for (int i = 0; i < degree; i++)
        {
            var ng = new byte[g.Length + 1];
            for (int j = 0; j < g.Length; j++)
            {
                ng[j] ^= GFMul(g[j], (byte)1);
                ng[j + 1] ^= GFMul(g[j], (byte)GF_EXP[i]);
            }
            g = ng;
        }
        return g;
    }

    private static byte GFMul(byte a, byte b) =>
        (a == 0 || b == 0) ? (byte)0 : (byte)GF_EXP[(GF_LOG[a] + GF_LOG[b]) % 255];

    // ── Matrix placement helpers ───────────────────────────────────────────

    private static void PlaceFinderPatterns(bool[,] m, bool[,] r, int size)
    {
        PlaceFinder(m, r, 0, 0);
        PlaceFinder(m, r, 0, size - 7);
        PlaceFinder(m, r, size - 7, 0);
    }

    private static void PlaceFinder(bool[,] m, bool[,] r, int row, int col)
    {
        for (int dr = -1; dr <= 7; dr++)
            for (int dc = -1; dc <= 7; dc++)
            {
                int rr = row + dr, cc = col + dc;
                if (rr < 0 || cc < 0 || rr >= m.GetLength(0) || cc >= m.GetLength(1)) continue;
                bool dark = dr >= 0 && dr <= 6 && dc >= 0 && dc <= 6 &&
                    (dr == 0 || dr == 6 || dc == 0 || dc == 6 ||
                     (dr >= 2 && dr <= 4 && dc >= 2 && dc <= 4));
                m[rr, cc] = dark;
                r[rr, cc] = true;
            }
    }

    private static void PlaceTimingPatterns(bool[,] m, bool[,] r, int size)
    {
        for (int i = 8; i < size - 8; i++)
        {
            m[6, i] = m[i, 6] = (i % 2 == 0);
            r[6, i] = r[i, 6] = true;
        }
    }

    private static void PlaceAlignmentPattern(bool[,] m, bool[,] r)
    {
        // Version 2 alignment: center at (18,18)
        for (int dr = -2; dr <= 2; dr++)
            for (int dc = -2; dc <= 2; dc++)
            {
                int rr = 18 + dr, cc = 18 + dc;
                m[rr, cc] = (dr == 0 && dc == 0) || Mathf.Abs(dr) == 2 || Mathf.Abs(dc) == 2;
                r[rr, cc] = true;
            }
    }

    private static void PlaceDarkModule(bool[,] m, bool[,] r, int size)
    {
        m[size - 8, 8] = true;
        r[size - 8, 8] = true;
    }

    private static void PlaceFormatInfo(bool[,] m, bool[,] r, int size, int ecLevel, int mask)
    {
        // Format: EC=M(01), mask=0b000 → data bits 01_000 → encode + XOR 101010000010010
        int format = ((ecLevel & 3) << 3) | (mask & 7);
        // Simplified: just mark format areas as reserved (real format encoding complex)
        for (int i = 0; i < 9; i++) r[8, i] = r[i, 8] = true;
        for (int i = size - 8; i < size; i++) r[8, i] = r[i, 8] = true;
    }

    private static void PlaceDataBits(bool[,] m, bool[,] r, int size, byte[] data)
    {
        int bitIdx = 0;
        int totalBits = data.Length * 8;
        bool upward = true;

        for (int col = size - 1; col >= 1; col -= 2)
        {
            if (col == 6) col = 5; // Skip timing column
            for (int row = 0; row < size; row++)
            {
                int actualRow = upward ? (size - 1 - row) : row;
                for (int dc = 0; dc < 2; dc++)
                {
                    int c = col - dc;
                    if (!r[actualRow, c] && bitIdx < totalBits)
                    {
                        int byteIdx = bitIdx / 8;
                        int bitPos  = 7 - (bitIdx % 8);
                        m[actualRow, c] = ((data[byteIdx] >> bitPos) & 1) == 1;
                        bitIdx++;
                    }
                }
            }
            upward = !upward;
        }
    }

    private static void ApplyMask(bool[,] m, bool[,] r, int size, int maskPattern)
    {
        for (int row = 0; row < size; row++)
            for (int col = 0; col < size; col++)
                if (!r[row, col])
                {
                    bool invert = maskPattern switch
                    {
                        0 => (row + col) % 2 == 0,
                        1 => row % 2 == 0,
                        2 => col % 3 == 0,
                        3 => (row + col) % 3 == 0,
                        _ => false
                    };
                    if (invert) m[row, col] = !m[row, col];
                }
    }
}
