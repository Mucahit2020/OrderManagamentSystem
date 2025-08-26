 
#!/bin/bash
# RabbitMQ Setup Script

# Wait for RabbitMQ to be ready
sleep 10

# Enable management plugin
rabbitmq-plugins enable rabbitmq_management

# Create exchanges
rabbitmqadmin declare exchange name=order.events type=topic durable=true
rabbitmqadmin declare exchange name=inventory.events type=topic durable=true  
rabbitmqadmin declare exchange name=invoice.events type=topic durable=true

# Create queues
rabbitmqadmin declare queue name=order.created durable=true
rabbitmqadmin declare queue name=stock.reduced durable=true
rabbitmqadmin declare queue name=stock.failed durable=true
rabbitmqadmin declare queue name=order.completed durable=true
rabbitmqadmin declare queue name=order.failed durable=true
rabbitmqadmin declare queue name=invoice.created durable=true
rabbitmqadmin declare queue name=invoice.failed durable=true

# Create bindings
rabbitmqadmin declare binding source=order.events destination=order.created routing_key=order.created
rabbitmqadmin declare binding source=inventory.events destination=stock.reduced routing_key=stock.reduced
rabbitmqadmin declare binding source=inventory.events destination=stock.failed routing_key=stock.failed
rabbitmqadmin declare binding source=order.events destination=order.completed routing_key=order.completed
rabbitmqadmin declare binding source=order.events destination=order.failed routing_key=order.failed
rabbitmqadmin declare binding source=invoice.events destination=invoice.created routing_key=invoice.created
rabbitmqadmin declare binding source=invoice.events destination=invoice.failed routing_key=invoice.failed

echo "RabbitMQ setup completed successfully!"