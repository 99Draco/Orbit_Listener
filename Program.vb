Imports System
Imports System.Linq.Expressions
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Data.SqlTypes

Module Program
    Sub Main(args As String())
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
                        Console.WriteLine("F {0}", data)
                    ElseIf data.Contains("$J") Then
                        Console.WriteLine("J {0}", data)
                    End If
                    'Anschlieﬂend  gibst du die antwort:
                    data = data
                    Dim msg As Byte() = System.Text.Encoding.ASCII.GetBytes(data)
                    stream.Write(msg, 0, msg.Length)
                    Console.WriteLine("Sent: {0}", data)
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
End Module
