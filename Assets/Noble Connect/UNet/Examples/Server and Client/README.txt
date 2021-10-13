Example scene that demonstrates connecting a custom networking solution using NobleServer and NobleClient.

This is intended to function identically to Unity's NetworkServer and NetworkClient.

Press the "Host" button to start hosting and display the host's IP and port.
To join a host, enter the host's IP and port in the GUI textboxes and press the "Connect" button.

The connection type will be displayed on the client:
DIRECT - The connection was made directly to the host's IP.
PUNCHTHROUGH - The connection was made to an address on the host's router discovered via punchthrough.
RELAY - The connection is using the Noble Connect relays.