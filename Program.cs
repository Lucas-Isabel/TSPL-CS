using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

class Program
{
    static void Main()
    {
        Dictionary<string, string> substituicoes = new Dictionary<string, string>
        {
            //{"ç", "c" },
            //{"Ç", "C" },
            { "$Produto", "çÇãÃâÂôõÕêÊ" },
            {"$VALIDADE", "10" },
            {"$DATA", DateTime.Now.ToShortDateString() },
            {"$PESO","10,000 kg" },
            {"$TARA","1,000 kg" },
            {"$PLU","1" },
            {"$TOTAL","10,00" },
            {"$PRECO", "29,99" },
            {"$LOJA", "Supermercado XYZ" }
        };

        bool resultado = SubstituirTextoNoArquivo("C:\\Users\\MAXWELL\\Desktop\\60X40_teste.prn", "C:\\Users\\MAXWELL\\Desktop\\60X40_2.prn", substituicoes);

        if (resultado)
        {
            Console.WriteLine("Substituições realizadas com sucesso.");
        }
        else
        {
            Console.WriteLine("Ocorreu um erro ao realizar as substituições.");
        }

        string filePath = "C:\\Users\\MAXWELL\\Desktop\\60X40_2.prn";

        // Ler o arquivo como ANSI e converter para UTF-8
        string fileContent = File.ReadAllText(filePath, Encoding.Default);
        byte[] prnBytes = Encoding.Latin1.GetBytes(fileContent);

        // Enviar os bytes diretamente para a impressora
        IntPtr pBytes = Marshal.AllocHGlobal(prnBytes.Length);
        Marshal.Copy(prnBytes, 0, pBytes, prnBytes.Length);

        bool result = RawPrinterHelper.SendBytesToPrinter("Xprinter XP-370BM", pBytes, prnBytes.Length);
        Marshal.FreeHGlobal(pBytes);

        if (result)
        {
            Console.WriteLine("Arquivo enviado com sucesso para a impressora.");
        }
        else
        {
            Console.WriteLine("Falha ao enviar o arquivo para a impressora.");
        }

    }
    static bool SubstituirTextoNoArquivo(string arquivoOriginal, string arquivoCopia, Dictionary<string, string> substituicoes)
    {
        try
        {
            // Lê o conteúdo do arquivo original
            string conteudo = File.ReadAllText(arquivoOriginal);

            // Realiza todas as substituições definidas no dicionário
            foreach (var substituicao in substituicoes)
            {
                conteudo = conteudo.Replace(substituicao.Key, substituicao.Value);
            }


            foreach (var substituicao in substituicoes)
            {
                conteudo = conteudo.Replace(substituicao.Key, substituicao.Value);
            }

            File.WriteAllText(arquivoCopia, conteudo);

            // Se tudo deu certo, retorna true
            return true;
        }
        catch (Exception ex)
        {
            // Em caso de erro, exibe a mensagem e retorna false
            Console.WriteLine($"Erro: {ex.Message}");
            return false;
        }
    }

    static int CalcularDVEAN13(string eanBase)
    {
        int somaImpares = 0;
        int somaPares = 0;

        for (int i = 0; i < eanBase.Length; i++)
        {
            int digito = int.Parse(eanBase[i].ToString());

            if (i % 2 == 0)
            {
                somaImpares += digito;
            }
            else
            {
                somaPares += digito;
            }
        }

        int somaTotal = somaImpares + (somaPares * 3);
        int dv = (10 - (somaTotal % 10)) % 10;

        return dv;
    }
    static byte[] GerarQRCodeBKP(string qrData)
    {
        int storeLen = qrData.Length + 3;
        byte storePL = (byte)(storeLen % 256);
        byte storePH = (byte)(storeLen / 256);

        // QR Code: Select the model
        byte[] modelQR = { (byte)0x1d, (byte)0x28, (byte)0x6b, (byte)0x04, (byte)0x00, (byte)0x31, (byte)0x41, (byte)0x32, (byte)0x00 };

        // QR Code: Set the size of the module
        byte[] sizeQR = { (byte)0x1d, (byte)0x28, (byte)0x6b, (byte)0x03, (byte)0x00, (byte)0x31, (byte)0x43, (byte)0x06 };

        // QR Code: Set error correction level
        byte[] errorQR = { (byte)0x1d, (byte)0x28, (byte)0x6b, (byte)0x03, (byte)0x00, (byte)0x31, (byte)0x45, (byte)0x31 };

        // QR Code: Store the data in the symbol storage area
        byte[] storeQR = { (byte)0x1d, (byte)0x28, (byte)0x6b, storePL, storePH, (byte)0x31, (byte)0x50, (byte)0x30 };

        // QR Code: Print the symbol data in the symbol storage area
        byte[] printQR = { (byte)0x1d, (byte)0x28, (byte)0x6b, (byte)0x03, (byte)0x00, (byte)0x31, (byte)0x51, (byte)0x30 };

        // Concatenate all command bytes
        List<byte> commands = new List<byte>();
        commands.AddRange(modelQR);
        commands.AddRange(sizeQR);
        commands.AddRange(errorQR);
        commands.AddRange(storeQR);
        commands.AddRange(Encoding.ASCII.GetBytes(qrData));
        commands.AddRange(printQR);

        // Convert list to array
        return commands.ToArray();
    }
}

public class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    public static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, int dwCount)
    {
        IntPtr hPrinter;
        DOCINFOA di = new DOCINFOA();
        di.pDocName = "ESC POS";
        di.pDataType = "RAW";
        bool success = false;

        if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
        {
            if (StartDocPrinter(hPrinter, 1, di))
            {
                if (StartPagePrinter(hPrinter))
                {
                    success = WritePrinter(hPrinter, pBytes, dwCount, out _);
                    EndPagePrinter(hPrinter);
                }
                EndDocPrinter(hPrinter);
            }
            ClosePrinter(hPrinter);
        }

        return success;
    }

}
