# 🚀 Ինտերնետում տեղադրման ուղեցույց (Render.com)

Այս ուղեցույցով քո project-ը կդառնա հասանելի ինտերնետով՝ անվճար։
Defense-ի ժամանակ կարող ես ուղղակի լինկ ուղարկել հանձնաժողովին։

## Ինչ ենք օգտագործելու

- **Render.com** — անվճար hosting (web service + PostgreSQL)
- **GitHub** — կոդի պահեստ (Render-ը այստեղից է վերցնում)
- Կրեդիտ քարտ ՊԵՏՔ ՉԷ

## ⚠️ Կարևոր

Локал-ում project-ը աշխատում է **SQL Server**-ով, իսկ Render-ում՝ **PostgreSQL**-ով։
Կոդը ավտոմատ ընտրում է ճիշտ provider-ը՝
- Եթե կա `DATABASE_URL` environment variable (Render) → PostgreSQL
- Հակառակ դեպքում (քո համակարգիչ) → SQL Server LocalDB

Ոչ մի բան փոխելու կարիք չկա — ամեն ինչ արդեն կարգավորված է։

---

## Քայլ 1. GitHub-ում կոդը տեղադրել

1. Գնա https://github.com → մուտք գործիր (կամ գրանցվիր)
2. Սեղմիր **New repository** (կանաչ կոճակ)
3. Անուն տուր՝ օրինակ `gasstation-ms`, թող լինի **Public**
4. Սեղմիր **Create repository**

Հետո քո համակարգչում, project-ի թղթապանակում (որտեղ `GasStationMS.csproj`-ն է)՝

```bash
git init
git add .
git commit -m "GasStation MS - նախնական տարբերակ"
git branch -M main
git remote add origin https://github.com/ՔՈ-ՕԳՏԱՆՈՒՆԸ/gasstation-ms.git
git push -u origin main
```

> 💡 Եթե git տեղադրված չէ՝ բեռնիր https://git-scm.com/downloads
> 💡 Push-ի ժամանակ կպահանջվի GitHub-ի գաղտնաբառ/token

---

## Քայլ 2. Render.com-ում գրանցվել

1. Գնա https://render.com
2. Սեղմիր **Get Started** → **Sign up with GitHub** (ամենահեշտն է)
3. Թույլատրիր Render-ին կարդալ քո GitHub repository-ները

---

## Քայլ 3. Deploy անել (ամենահեշտ ձևը՝ Blueprint)

Project-ում արդեն կա `render.yaml` ֆայլ, որը ավտոմատ կարգավորում է ամեն ինչ։

1. Render-ի dashboard-ում սեղմիր **New +** → **Blueprint**
2. Ընտրիր քո `gasstation-ms` repository-ն
3. Render-ը կկարդա `render.yaml`-ը ու ցույց կտա՝
   - **gasstation-db** (PostgreSQL database)
   - **gasstation-ms** (web service)
4. Սեղմիր **Apply**
5. Սպասիր 5-10 րոպե (առաջին build-ը երկար է տևում)

Երբ ավարտվի, կտեսնես կանաչ **Live** կարգավիճակ ու լինկ՝
`https://gasstation-ms.onrender.com` (կամ նման)

---

## Քայլ 4. Բացել և մուտք գործել

Բացիր լինկը բրաուզերում։ Կտեսնես landing page-ը։
Սեղմիր **Մուտք** ու մուտք գործիր՝

| Email | Գաղտնաբառ |
|---|---|
| admin@gasstation.am | Admin@12345 |

Համակարգը առաջին գործարկման ժամանակ ինքնաշխատորեն կստեղծի
բոլոր seed տվյալները (կայաններ, ռեզերվուարներ, հաճախորդներ և այլն)։

---

## ⏱ Կարևոր՝ "Sleep" ռեժիմ

Անվճ tier-ում, եթե 15 րոպե ոչ ոք չի օգտվում, service-ը "քնում է"։
Հաջորդ բացման ժամանակ առաջին հարցումը կտևի **30-50 վայրկյան**
(service-ը արթնանում է)։ Հետո ամեն ինչ արագ է։

💡 **Defense-ից առաջ** մի 2-3 րոպե առաջ բացիր լինկը, որ service-ը
արդեն արթուն լինի, երբ հանձնաժողովին ցույց տաս։

---

## 🔄 Կոդը թարմացնելու դեպքում

Եթե փոխում ես կոդը՝

```bash
git add .
git commit -m "փոփոխությունների նկարագրություն"
git push
```

Render-ը ավտոմատ կբռնի push-ը ու նորից կdeploy անի։ Ուրիշ բան պետք չէ։

---

## 🛠 Խնդիրների լուծում

**Build-ը ձախողվում է** — բացիր Render-ի **Logs** էջը, կարդա սխալը։
Սովորաբար package-ի version-ի խնդիր է լինում։

**Էջը չի բացվում / 502 սխալ** — սպասիր 1 րոպե (service-ը արթնանում է)։
Եթե չի օգնում՝ Render dashboard → Logs, ստուգիր սխալները։

**Տվյալների բազան դատարկ է** — առաջին գործարկման ժամանակ seed-ը
ավտոմատ աշխատում է։ Եթե չստացվեց, Render-ում Manual Deploy արա կրկին։

---

## Այլընտրանք՝ Azure for Students (եթե ունես համալսարանի email)

Եթե ունես համալսարանի email (.edu կամ հայկական համալսարանի email),
կարող ես օգտվել **Azure for Students**-ից՝ 100$ անվճ կրեդիտով, որը
թույլ է տալիս օգտագործել իրական **SQL Server** (Azure SQL Database)՝
առանց կոդի փոփոխության։ Բայց Render-ը ավելի պարզ է սկսնակների համար։
