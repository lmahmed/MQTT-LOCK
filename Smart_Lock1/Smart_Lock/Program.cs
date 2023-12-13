using MQTTnet;
using MQTTnet.Client;
using System.Data.SqlTypes;
using System.Text.Json;

namespace Smart_Lock
{
    public static class Program
    {
        static public string main_password = "abcd";
        static public string temp_password = "efgh";
        static public string lock_file = "lock.txt";
        static public bool tempActivated = false;

        static async Task Main(string[] args)
        {
            await RunClient();
        }

        static async Task RunClient()
        {
            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1").WithWillTopic("smartlock/response").WithWillPayload("The lock was broken or died").Build();

                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    bool locked;
                    // where lock status is held
                    using (StreamReader file = File.OpenText(lock_file))
                    {
                        locked = int.Parse(file.ReadLine()) == 1;
                    }

                    string client_message = e.ApplicationMessage.ConvertPayloadToString();
                    string lock_message;
                    switch (e.ApplicationMessage.Topic)
                    {
                        case "smartlock/lock":
                            if (locked)
                            {
                                lock_message = "Lock is already locked";
                            }
                            else
                            {
                                if (client_message == main_password)
                                {
                                    using (StreamWriter file = new StreamWriter(lock_file))
                                    {
                                        file.Write('1');
                                    }
                                    lock_message = "Lock has been locked with permanent password";
                                }
                                else if (client_message == temp_password)
                                {
                                    if (tempActivated)
                                    {
                                        using (StreamWriter file = new StreamWriter(lock_file))
                                        {
                                            file.Write('1');
                                        }
                                        lock_message = "Lock has been locked with temporary password";
                                        tempActivated = false;
                                    }
                                    else
                                    {
                                        lock_message = "Temp password is deactivated. Lock has not been locked";
                                    }
                                }
                                else
                                {
                                    lock_message = "Password does not match permanent or temporary password";
                                }
                            }
                            break;
                        case "smartlock/unlock":
                            if (!locked)
                            {
                                lock_message = "Lock is already unlocked";
                            }
                            else
                            {
                                if (client_message == main_password)
                                {
                                    using (StreamWriter file = new StreamWriter(lock_file))
                                    {
                                        file.Write('0');
                                    }
                                    lock_message = "Lock has been unlocked with permanent password";
                                }
                                else if (client_message == temp_password)
                                {
                                    if (tempActivated)
                                    {
                                        using (StreamWriter file = new StreamWriter(lock_file))
                                        {
                                            file.Write('0');
                                        }
                                        lock_message = "Lock has been unlocked with temporary password";
                                        tempActivated = false;
                                    }
                                    else
                                    {
                                        lock_message = "Temp password is deactivated. Lock has not been unlocked";
                                    }
                                }
                                else
                                {
                                    lock_message = "Password does not match permanent or temporary password";
                                }
                            }
                            break;
                        case "smartlock/status":
                            if (locked)
                            {
                                lock_message = "Lock is currently locked";
                            }
                            else
                            {
                                lock_message = "Lock is currently unlocked";
                            }
                            break;
                        case "smartlock/temporary/activate":
                            if (tempActivated)
                            {
                                lock_message = "Temporary password is already activated";
                            }
                            else
                            {
                                if (client_message == main_password)
                                {
                                    lock_message = "Temporary password has been activated";
                                    tempActivated = true;
                                }
                                else
                                {
                                    lock_message = "Password does not match permanent password. Temporary password has not been activated.";
                                }
                            }
                            break;
                        case "smartlock/temporary/deactivate":
                            if (!tempActivated)
                            {
                                lock_message = "Temporary password is already deactivated";
                            }
                            else
                            {
                                if (client_message == main_password)
                                {
                                    lock_message = "Temporary password has been deactivated";
                                    tempActivated = false;
                                }
                                else
                                {
                                    lock_message = "Password does not match permanent password. Temporary password has not been deactivated.";
                                }
                            }
                            break;
                        default:
                            lock_message = "Invalid topic";
                            break;
                    }

                    var applicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic("smartlock/response")
                            .WithPayload(lock_message)
                            .Build();

                    mqttClient.PublishAsync(applicationMessage, CancellationToken.None).Wait();

                    return Task.CompletedTask;
                };

                // This will throw an exception if the server is not available.
                // The result from this message returns additional data which was sent 
                // from the server. Please refer to the MQTT protocol specification for details.
                var response = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                Console.WriteLine("The MQTT client is connected.");

                var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic("smartlock/lock");
                    })
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic("smartlock/status");
                    })
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic("smartlock/temporary/+");
                    })
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic("smartlock/unlock");
                    })
                .Build();
                var response2 = await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

                Console.WriteLine("MQTT client subscribed to topic.");


                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // Send a clean disconnect to the server by calling _DisconnectAsync_. Without this the TCP connection
                // gets dropped and the server will handle this as a non clean disconnect (see MQTT spec for details).
                var mqttClientDisconnectOptions = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();

                await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);
            }
        }

        public static TObject DumpToConsole<TObject>(this TObject @object)
        {
            var output = "NULL";
            if (@object != null)
            {
                output = JsonSerializer.Serialize(@object, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }

            Console.WriteLine($"[{@object?.GetType().Name}]:\r\n{output}");
            return @object;
        }
    }
}
