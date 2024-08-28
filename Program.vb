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
    Sub Main(args As String())
        'TCP Listener
        Dim server As TcpListener
        server = Nothing
        Try
            ' Set the TcpListener on port 13000.
            Dim port As Int32 = 5000
            Dim localAddr As IPAddress = IPAddress.Parse("192.168.1.109")
            server = New TcpListener(localAddr, port)
            ' Start listening for client requests.
            server.Start()
            ' Buffer for reading data
            Dim bytes(1024) As Byte
            Dim data As String = Nothing
            ' Enter the listening loop.
            Console.WriteLine("Locale IP-Adresse: {0}", localAddr)
            Console.WriteLine("Listening Port: {0}", port)
            Console.Write("Waiting for a connection... ")
            Dim client As TcpClient = server.AcceptTcpClient()
            Console.WriteLine("Connected!")
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
                        Loxonde_sender(message)
                        If split_komma(data) = "Finish" Then
                            Finish = True
                        Else
                            Finish = False
                        End If
                    ElseIf data.Contains("$J") Then
                        If Finish Then
                            afterFinish += 1
                            Dim message As String = "count:" + afterFinish
                            Console.WriteLine(message)
                            Loxonde_sender(message)
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

    Function split_komma(ByVal str As String) As String
        Dim ar As Array = str.Split(", ")
        Return ar(ar.Length - 1)
    End Function

End Module
