# Feature/POC �al��malar�

K�kl� de�i�ikli�e sebep olabilecek �zellikler veaya ara�t�rmalar ile ilgili a��lan alt branch'lere ait detayl� bilgiler.

## System HAL Servisinin Ayr��t�r�lmas�

System HAL i�erisinde yer alan Audit servisinin DistributedChallenges solution'� d���na ��kart�lmas� ve Dockerize edilerek i�letilmesi i�in ba�lat�lan �al��mad�r. 

Audit servis temsilen aray�zden gelen bir rapor talebindeki ifadeyi denetlemek i�in kullan�lan REST tabanl� bir .Net servisidir. Bu servis i�erisinde kullan�lan baz� paket ba��ml�l�klar� bulunmaktad�r.

- Resistance; Resilience deneyleri i�in kullan�lan ve local ortamda depolanan nuget paketidir.
- JudgeMiddleware; Baz� performans ve girdi ��kt� loglamalar� i�in kullan�lan ve local ortamda depolanan nuget paketidir.
- Consul; Servisin Consul �zerinden ke�fedilebilmesi i�in kullan�lan nuget paketidir.

### Plan

�lk olarak SystemHAL i�erisindeki Eval.AuditLib i�eri�i Eval.AuditApi i�erisine al�n�p projenin lightweight bir versiyonu haz�rlan�r. Sonras�nda proje DistributedChallenge ��z�m�nden ayr��t�r�l�r. Yeni ��z�mdeki proje dockerize edilir. Burada kar��la��labilecek ve ��z�lmesi gereken baz� problemler s�z konusudur.

- **Local Nuget Repo Sorunu:** Dockerize edilen projeye ait container olu�turulurken container d���ndaki ama ana makinedeki BaGet server'�na eri�ip local nuget ba��ml�l�klar�n� ��z�mleyebilmelidir.
- **Consul Problemi:** Genel ��z�mde Service Discovery i�in kullan�lan Consul, ayr� bir docker-compose i�eri�i ile aya�la kald�r�lan farkl� bir container olarak �al��maktad�r. Dockerize edilen Eval.AuditApi servisi aya�a kalkarken kendisini Consul hizmetine bildirmektedir. Dockerize edilen servisin di�er docker container'�ndaki Consul hizmetine ba��ms�z olarak eri�ebiliyor olmas� gerekmektedir.

Yukar�da bahsedilen maddeler plan dahilinde ��z�mlenmesi gereken meselelerdir.

## ��lemler