Imports System
Imports System.Linq.Expressions
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Data.SqlTypes
Imports System.Net.Http
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices.JavaScript.JSType

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

    'TCP Variabeln
    Private stream As NetworkStream
    Private streamw As StreamWriter
    Private streamr As StreamReader
    Private Client As New System.Net.Sockets.TcpClient

    Sub Main(args As String())
        init()
        'TCP Listener
        Try
            Client.Connect(orbitIP, orbitPort)

            Dim data As String = Nothing
            If Client.Connected Then

                Deklare_Streams()



                'login() ' Sub Login
            End If
            If Client.Connected Then
                streamw.WriteLine("Test")
                Deklare_Streams()
                login()
                data = client_recieve()
                Console.WriteLine(data)
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
            End If
            ' Shutdown and end connection
            Client.Close()
        Catch e As SocketException
            Console.WriteLine("SocketException: {0}", e)
        Finally
            Client.Close()
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

    Sub Deklare_Streams()
        stream = Client.GetStream                      ' Stream wird auf Client 
        ' verwiesen
        streamw = New StreamWriter(stream)      ' Stream zum Senden wird 
        ' deklariert
        streamr = New StreamReader(stream)      ' Stream zum Empfangen wird 
        ' deklariert
    End Sub

    Sub login()
        Try
            'Hier kann man jetzt empfangen wie man will.
            'Auch die reinfolge ist egal.
            'Ich benutzte hier 1 mal senden, einmal empfangen und dann wieder 
            ' von vorne...
            'Man kann aber auch 2 mal Empfangen und 2 mal Senden nehmen 
            ' oder...... wies einem so bekommt^^
            'client_send("onl ") '&  loginname)
            '            client_send(TBox_Senden.Text)

            Dim Zum_Senden(4) As String
            Zum_Senden = {"0x7c", "0x04", "0x00", "0x00"}
            For i As Integer = 0 To 3
                client_send(Zum_Senden(i))
                streamw.Flush()
            Next


            MsgBox("client_recieve() = " & client_recieve())
            Client.Close() ' Nichts einfacher als das


        Catch
            ' Hier kann man eine Error Message ausgeben oder eine Automatische 
            ' Fehlerbehebung machen,....
            ' Verbindung beenden
            Client.Close() ' Nichts einfacher als das

            MsgBox("Fehlernr.: " & Err.Number & "   " & Err.Description)
        End Try
    End Sub

    Sub client_send(ByVal text As String)
        streamw.WriteLine(text)

        'streamw.Flush()
    End Sub
    Function client_recieve() As String
        client_recieve = streamr.ReadLine
    End Function
End Module
