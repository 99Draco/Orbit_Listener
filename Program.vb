Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.IO
Imports System.Threading
Imports System.Security.Cryptography
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.Marshalling

Module Program
    Private afterFinish As Integer = 0
    Private Finish As Boolean = False

    'IP und Port Orbit
    Private orbitIP As String
    Private orbitPort As Int32 = 50000

    'IP und Ports Loxone
    Private loxoneIP As IPAddress
    Private loxonePort As Int32 = 1234

    'TCP Variabeln
    Private TcpClientReceiverThread As New Threading.Thread(AddressOf ClientReceiverThread)
    Private exiting As Boolean = True
    Sub Main(args As String())
        init()
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
        Console.WriteLine("Loxone Port: ")
        loxonePort = Console.ReadLine
    End Sub
    Private Function split_komma(ByVal str As String) As String
        Dim ar As Array = str.Split(",")
        Return ar(ar.Length - 1)
    End Function
    Sub Loxonde_sender(ByVal strMessage As String)
        Dim client As New UdpClient()
        Dim ip As New IPEndPoint(loxoneIP, loxonePort)
        Try
            Dim bytSent As Byte() = Encoding.ASCII.GetBytes(strMessage)
            client.Send(bytSent, ip)
            client.Close()

        Catch e As Exception

            Console.WriteLine(e.ToString())
        End Try
    End Sub
    Private Async Sub ClientReceiverThread()
        Try

            Dim client As New TcpClient(orbitIP, orbitPort)

            Dim data As Byte() = System.Text.Encoding.ASCII.GetBytes("conect")

            Dim stream As NetworkStream = client.GetStream

            stream.Write(data, 0, data.Length)

            data = New Byte(256) {}

            Dim responseData As String = String.Empty

            Do While exiting

                Dim bytes As Int32 = stream.Read(data, 0, data.Length)
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes)

                'Console.WriteLine(responseData)

                If responseData.Contains("$F") Then
                    Console.WriteLine("F {0}", responseData)
                    Dim message As String = "status:" + split_komma(responseData)
                    Console.WriteLine(message)
                    Loxonde_sender(message)
                    If message.Contains("Finish") Then
                        Finish = True
                    Else
                        Finish = False
                    End If
                ElseIf responseData.Contains("$J") Then
                    Console.WriteLine("J {0}", responseData)
                    If Finish = True Then
                        afterFinish += 1
                        Dim message As String = "count: " & afterFinish
                        Console.WriteLine(message)
                        Loxonde_sender(message)
                    End If
                ElseIf responseData.Contains("$B") Then
                    afterFinish = 0
                    Finish = False
                End If
            Loop
        Catch ex As Exception                               'If we get an exception at any point in the process (usually a connection lost or closed)
        Finally
        End Try
        exiting = True
    End Sub
End Module
