```sh
podman compose -f 'docker-compose.yml' up -d --build 'rabbitmq'

podman compose -f 'docker-compose.yml' up -d --build 'yugabytedb'

podman compose -f 'docker-compose.yml' up -d --build 'orderbookapi'

podman compose -f 'docker-compose.yml' up -d  --build 'orderbookworker'
```

```sh
k6 run teste_5000rps.js

```