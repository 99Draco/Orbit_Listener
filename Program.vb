Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.IO
Imports System.Threading
Imports System.Collections.Generic
Imports Microsoft.VisualBasic.FileIO

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
    Private running As Boolean = True
    Private Const ReconnectDelayMs As Integer = 5000

    'Logging
    Private Const LogFilePath As String = ".\Orbit_Listener.log"
    Private ReadOnly logLock As New Object()

    Sub Main(args As String())
        init()
        TcpClientReceiverThread.IsBackground = True
        TcpClientReceiverThread.Start()
        TcpClientReceiverThread.Join()
    End Sub

    Sub init()
        ReadConfig()
    End Sub
    Private Function split_komma(ByVal str As String) As String
        Dim ar As String() = str.Split(","c)
        Return ar(ar.Length - 1)
    End Function

    Sub Log(ByVal message As String)
        Dim line As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}"
        Console.WriteLine(line)
        Try
            SyncLock logLock
                File.AppendAllText(LogFilePath, line & Environment.NewLine)
            End SyncLock
        Catch
            'Ein fehlgeschlagenes Log-Schreiben darf das Programm nicht zum Absturz bringen
        End Try
    End Sub

    Sub Loxonde_sender(ByVal strMessage As String)
        Try
            Using client As New UdpClient()
                Dim ip As New IPEndPoint(loxoneIP, loxonePort)
                Dim bytSent As Byte() = Encoding.ASCII.GetBytes(strMessage)
                client.Send(bytSent, ip)
            End Using
        Catch e As Exception
            Log(e.ToString())
        End Try
    End Sub

    ''' Verarbeitet eine einzelne, vollständige Nachricht (eine Zeile) vom Orbit-Server.
    Private Sub ProcessMessage(ByVal responseData As String)
        If responseData.Contains("$F") Then
            Log($"F {responseData}")
            Dim message As String = "status:" + split_komma(responseData)
            Log(message)
            Loxonde_sender(message)
            Finish = message.Contains("Finish")
        ElseIf responseData.Contains("$J") Then
            Log($"J {responseData}")
            If Finish Then
                afterFinish += 1
                Dim message As String = "count: " & afterFinish
                Log(message)
                Loxonde_sender(message)
            End If
        ElseIf responseData.Contains("$B") Then
            afterFinish = 0
            Finish = False
        End If
    End Sub

    Private Sub ClientReceiverThread()
        Do While running
            Try
                Using client As New TcpClient(orbitIP, orbitPort)
                    Using stream As NetworkStream = client.GetStream()
                        Log($"Verbunden mit Orbit {orbitIP}:{orbitPort}")

                        Dim data As Byte() = Encoding.ASCII.GetBytes("conect")
                        stream.Write(data, 0, data.Length)

                        data = New Byte(256) {}

                        'Puffer für Nachrichten, die sich über mehrere TCP-Reads erstrecken oder
                        'zu mehreren pro Read eintreffen (TCP kennt keine Nachrichtengrenzen).
                        Dim receiveBuffer As New StringBuilder()

                        Do While running
                            Dim bytes As Int32 = stream.Read(data, 0, data.Length)
                            If bytes = 0 Then
                                Throw New IOException("Verbindung wurde vom Orbit-Server geschlossen.")
                            End If
                            receiveBuffer.Append(Encoding.ASCII.GetString(data, 0, bytes))

                            Dim lines() As String = receiveBuffer.ToString().Split(Chr(10))
                            For i As Integer = 0 To lines.Length - 2
                                Dim line As String = lines(i).Trim(Chr(13))
                                If line.Length > 0 Then
                                    ProcessMessage(line)
                                End If
                            Next

                            'Der letzte Teil kann eine noch unvollständige Nachricht sein - im Puffer behalten
                            receiveBuffer.Clear()
                            receiveBuffer.Append(lines(lines.Length - 1))
                        Loop
                    End Using
                End Using
            Catch ex As Exception                               'Verbindung verloren oder Verbindungsaufbau fehlgeschlagen
                Log($"Verbindung zu Orbit verloren: {ex.Message}")
            End Try

            If running Then
                Log($"Erneuter Verbindungsversuch in {ReconnectDelayMs \ 1000} Sekunden...")
                Thread.Sleep(ReconnectDelayMs)
            End If
        Loop
    End Sub

    Sub ReadConfig()
        Dim FilePath As String = ".\config.txt"

        Try
            Dim fileContent As String = FileSystem.ReadAllText(FilePath)
            Dim configLine() As String = Split(fileContent, vbCrLf)
            Dim configDict As New Dictionary(Of String, String)()

            For i As Integer = LBound(configLine) To UBound(configLine)
                Dim keyValue() As String = Split(configLine(i), "=")
                If UBound(keyValue) = 1 Then
                    configDict(keyValue(0)) = keyValue(1)
                End If
            Next

            RequireKey(configDict, "OrbitsIP")
            RequireKey(configDict, "OrbitsPort")
            RequireKey(configDict, "LoxoneIP")
            RequireKey(configDict, "LoxonePort")

            orbitIP = configDict("OrbitsIP")
            orbitPort = Integer.Parse(configDict("OrbitsPort"))
            loxoneIP = IPAddress.Parse(configDict("LoxoneIP"))
            loxonePort = Integer.Parse(configDict("LoxonePort"))

            Log($"Konfiguration geladen: Orbit={orbitIP}:{orbitPort}, Loxone={loxoneIP}:{loxonePort}")
        Catch ex As Exception
            Log($"Fehler beim Lesen von config.txt: {ex.Message}")
            Environment.Exit(1)
        End Try
    End Sub

    Private Sub RequireKey(configDict As Dictionary(Of String, String), key As String)
        Dim value As String = Nothing
        If Not configDict.TryGetValue(key, value) OrElse value.Length = 0 Then
            Throw New InvalidDataException($"Fehlender oder leerer Konfigurationswert: {key}")
        End If
    End Sub
End Module
