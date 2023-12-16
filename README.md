A MQTT client, representing a smartlock, that subscribes to certain topics to implement locking/unlocking of the lock and activating/deactivating a temporary password. The expected payloads to the locking/unlocking topics are the permanent password or the temporary password(if activated). The expected payload to the activating/deactivating temporary password topics is the permanent password. The state of the lock is stored in a txt file. A 0 in the file represents that the smartlock is unlocked and a 1 represents that the smartlock is locked.  The smartlock also publishes a reply message to a response topic when it receives a message. To this same topic, the smartlock sends a last will testament message.

To test, we ran a localhost MQTT broker (mosquitto). We used mosquitto_sub to subscribe to the topic the smartlock publishes to.  We used mosquitto_pub to publish to the topics the smartlock is subscribed to. In these publish messages, we tested different payloads and different situations, like trying to lock the smartlock while it is already locked.
