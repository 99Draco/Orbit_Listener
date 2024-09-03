Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.IO
Imports System.Threading
Imports System.Security.Cryptography

Module Program
    Private afterFinish As Integer = 0
    Private Finish As Boolean = False

    'IP und Port Orbit
    Private orbitIP As String = "192.168.1.109"
    Private orbitPort As Int32 = 50000

    'IP und Ports Loxone
    Private loxoneIP As IPAddress = IPAddress.Parse("192.168.1.109")
    Private loxoneFlagPort As Int32 = 1234
    Private loxoneCountPort As Int32 = 12345

    'TCP Variabeln
    Private TcpClientReceiverThread As New Threading.Thread(AddressOf ClientReceiverThread)
    Private exiting As Boolean = True
    Sub Main(args As String())
        'init()
        TcpClientReceiverThread.IsBackground = True
        TcpClientReceiverThread.Start()
        'TCP Listener
        Do While exiting

        Loop
    End Sub

    Sub init()
        Console.WriteLine("IP-Adresse Orbit: ")
        orbitIP = Console.ReadLine
        Console.WriteLine("Port Orbit: ")
        orbitPort = Console.ReadLine
        Console.WriteLine("IP-Adresse Loxone: ")
        loxoneIP = IPAddress.Parse(Console.ReadLine)
        Console.WriteLine("Loxone Flag Port: ")
        loxoneFlagPort = Console.ReadLine
        Console.WriteLine("Loxone Count Port: ")
        loxoneCountPort = Console.ReadLine
    End Sub
    Private Function split_komma(ByVal str As String) As String
        Dim ar As Array = str.Split(", ")
        Return ar(ar.Length - 1)
    End Function
    Sub Loxonde_sender(ByVal strMessage As String)
        Dim client As New UdpClient()
        Dim ip As New IPEndPoint(IPAddress.Parse("192.168.1.109"), 1234)
        Try
            Dim bytSent As Byte() = Encoding.ASCII.GetBytes(strMessage)
            client.Send(bytSent, bytSent.Length, ip)
            client.Close()

        Catch e As Exception

            Console.WriteLine(e.ToString())
        End Try
    End Sub
    Private Sub ClientReceiverThread()
        Dim client As New TcpClient

        Dim bLen(3) As Byte  'the len of the message being sent or received
        Dim outData(100) As Byte

        Dim iLen As Int32
        Dim rand As New Random

        'some random data to send from
        For i As Integer = 0 To 100
            outData(i) = CByte(rand.Next(1, 255))
        Next

        'Do While orbitPort = 50000    'Wait for Server to tell us what port to connect to
        'Thread.Sleep(100)
        ' Loop

        client.Connect(IPAddress.Parse(orbitIP), orbitPort)

        If client.Connected Then

            Dim nStream As NetworkStream = client.GetStream

            Try
                'Send a message to the server to get things started.
                iLen = rand.Next(10, 100)                    'choose to send 10 to 100 bytes to the server
                bLen = BitConverter.GetBytes(iLen)           'convert length to four bytes
                nStream.Write(bLen, 0, 4)                    'send length out first
                nStream.Write(outData, 0, iLen)              'followed by the data
                Console.WriteLine("Client sent the first message to kick things off. Sent {0} bytes", iLen)

                Dim cnt As Integer
                Do While exiting
                    If nStream.CanRead Then
                        cnt = nStream.Read(bLen, 0, 4)
                        Dim inData(100) As Byte
                        iLen = nStream.Read(inData, 0, iLen)
                        Dim daten As String = Encoding.ASCII.GetString(inData)
                        Console.WriteLine(daten)
                        If daten.Contains("$F") Then
                            Console.WriteLine("F {0}", daten)
                            Dim message As String = "status:" + split_komma(daten)
                            Console.WriteLine(message)
                            Loxonde_sender(message)
                            If split_komma(daten) = "Finish" Then
                                Finish = True
                            Else
                                Finish = False
                            End If
                        ElseIf daten.Contains("$J") Then
                            Console.WriteLine("J {0}", daten)
                            If Finish Then
                                afterFinish += 1
                                Dim message As String = "count:" + afterFinish
                                Console.WriteLine(message)
                                Loxonde_sender(message)
                            End If
                        ElseIf daten.Contains("$B") Then
                            afterFinish = 0
                            Finish = False
                        End If
                        iLen = rand.Next(10, 100)                    'choose to send 10 to 100 bytes the other direction
                        bLen = BitConverter.GetBytes(iLen)           'convert length to four bytes
                        nStream.Write(bLen, 0, 4)                    'send length out first
                        nStream.Write(outData, 0, iLen)
                    End If
                Loop
            Catch ex As Exception                               'If we get an exception at any point in the process (usually a connection lost or closed)
            Finally
                If nStream IsNot Nothing Then nStream.Dispose()
                If client IsNot Nothing Then client.Close()
            End Try

            client = Nothing
        Else
            Console.WriteLine("Client didn't connect. Example busted")
        End If
        exiting = True
    End Sub
End Module
