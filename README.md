# MQTT RabbitMQ Sample

This sample shows a very simple web application which handles MQTT messages from a RabbitMQ server.

## Requirements

This sample using .NET Core 3.1, MassTransit 6, and RabbitMQ, Docker.

The easiest way to get up and running is by using Docker Compose and running `docker-compose up` in the "src" directory.   
Your RabbitMQ server is listening on localhost:1883 and has credentials of `rabbitmq` for the username and password. This will give you a fully isolated instance for testing.
If you already have a RabbitMQ instance on port 1883, either stop it or change the port binding in the docker-compose file.

If you already have RabbitMQ running locally, you will need to add the MQTT plugin `rabbitmq-plugins enable rabbitmq_mqtt`.

## Connecting to RabbitMQ

The management portal can be found at http:localhost:15672

## Connecting IOT device to RabbitMQ

Ensure you have opened the port 1883 for all inbound TCP traffic on your computer.   
Connect your device to your local network and set the URL to `TCP://{machine IP}:1883` and username and password is `rabbitmq`
