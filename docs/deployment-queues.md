The **prdeploy** app keeps on pull request per environment, to minimize the changes needed to test and keep deployments running quickly.  Because there can be multiple engineers working from the same repository and set of services, if an envirionment is in use, a pull request will be added to the queue and then deployed automatically when the environment is free.

![Deployment queue comment](./assets/images/screenshots/deployment-queues.png)
{: style="margin: 50px 0 0 0;"}