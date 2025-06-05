import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 5000 },  // Aumenta gradualmente para 5000 RPS
    { duration: '1m', target: 5000 },    // Mantém 5000 RPS por 1 minuto
  ],
};

export default function () {
  let res = http.post('http://localhost:3000/orders/asks',
    {
        price: 10.50,
        quantity: 100000

    }); // Substitua pela sua URL
  check(res, { 'status was 200': (r) => r.status == 200 });
}