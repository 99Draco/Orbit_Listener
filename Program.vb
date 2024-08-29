Imports System
Imports System.Linq.Expressions
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Data.SqlTypes

Module Program
    Private afterFinish As Integer = 0
    Private Finish As Boolean = False

    'IP und Port Orbit
    Private orbitIP As IPAddress
    Private orbitPort As Int32

    'IP und Ports Loxone
    Private loxoneIP As IPAddress
    Private loxoneFlagPort As Int32
    Private loxoneCountPort As Int32
    Sub Main(args As String())
        init()
        'TCP Listener
        Dim server As TcpListener
        server = Nothing
        Try

            server = New TcpListener(orbitIP, orbitPort)
            ' Start listening for client requests.
            server.Start()
            ' Buffer for reading data
            Dim bytes(1024) As Byte
            Dim data As String = Nothing
            ' Enter the listening loop.
            Dim client As TcpClient = server.AcceptTcpClient()
            While True
                ' Perform a blocking call to accept requests.
                ' You could also user server.AcceptSocket() here.
                client = server.AcceptTcpClient()
                data = Nothing
                ' Get a stream object for reading and writing
                Dim stream As NetworkStream = client.GetStream()
                Dim i As Int32
                ' Loop to receive all the data sent by the client.
                i = stream.Read(bytes, 0, bytes.Length)
                While (i <> 0)
                    ' Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i)
                    Console.WriteLine("Received: {0}", data)
                    ' Hier hast du den empfangenen string data
                    If data.Contains("$F") Then
                        Dim message As String = "status:" + split_komma(data)
                        Console.WriteLine(message)
                        Loxonde_sender(message, loxoneFlagPort)
                        If split_komma(data) = "Finish" Then
                            Finish = True
                        Else
                            Finish = False
                        End If
                    ElseIf data.Contains("$J") Then
                        If Finish Then
                            afterFinish += 1
                            Dim message As String = "count:" & afterFinish
                            Console.WriteLine(message)
                            Loxonde_sender(message, loxoneCountPort)
                        End If
                    ElseIf data.Contains("$B") Then
                        afterFinish = 0
                        Finish = False
                    End If
                    'Anschlieﬂend  gibst du die antwort:
                    data = data
                    Dim msg As Byte() = System.Text.Encoding.ASCII.GetBytes(data)
                    stream.Write(msg, 0, msg.Length)
                    i = stream.Read(bytes, 0, bytes.Length)
                End While
                ' Shutdown and end connection
                client.Close()
            End While
        Catch e As SocketException
            Console.WriteLine("SocketException: {0}", e)
        Finally
            server.Stop()
        End Try
        Console.WriteLine(ControlChars.Cr + "Hit enter to continue....")
        Console.Read()
    End Sub

    Sub Loxonde_sender(ByVal strMessage As String, ByVal port As Int32)
        Dim client As New UdpClient()
        Dim ip As New IPEndPoint(loxoneIP, port)
        Try
            Dim bytSent As Byte() = Encoding.ASCII.GetBytes(strMessage)
            client.Send(bytSent, bytSent.Length, ip)
            client.Close()

        Catch e As Exception

            Console.WriteLine(e.ToString())
        End Try
    End Sub

    Function split_komma(ByVal str As String) As String
        Dim ar As Array = str.Split(", ")
        Return ar(ar.Length - 1)
    End Function

    Sub init()
        Console.WriteLine("IP-Adresse Orbit: ")
        orbitIP = IPAddress.Parse(Console.ReadLine())
        Console.WriteLine("Port Orbit: ")
        orbitPort = Console.ReadLine
        Console.WriteLine("IP-Adresse Loxone: ")
        loxoneIP = IPAddress.Parse(Console.ReadLine)
        Console.WriteLine("Loxone Flag Port: ")
        loxoneFlagPort = Console.ReadLine
        Console.WriteLine("Loxone Count Port: ")
        loxoneCountPort = Console.ReadLine
    End Sub

End Module
