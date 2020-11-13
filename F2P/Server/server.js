const express = require("express");
const bcrypt = require("bcrypt");
const jwt = require("jsonwebtoken");
const ObjectId = require("mongoose").Types.ObjectId;
const mysql = require("mysql2");
const cors = require("cors");
const app = express();

const jsonMiddleware = express.json();
app.use(jsonMiddleware);
app.use(cors());

let secret = process.env.JWT_SECRET;
const salt = 10;

// Ce middleware vérifie qu’un JWT valide est présent dans le header Authorization
const jwtMiddleware = require("express-jwt")({
		secret: secret
// sauf pour les routes permettant à un joueur de récupérer un JWT.
	}).unless({ path: ["/users/signup", "/users/login"]});

app.use(jwtMiddleware);

const db_infos = {
  host: process.env.DATABASE_HOST,
  user: process.env.DATABASE_USER,
  database: process.env.DATABASE_NAME,
  password: process.env.DATABASE_PASSWORD,
  port: process.env.DATABASE_PORT,
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 10
};

const pool = mysql.createPool(db_infos);
const promisePool = pool.promise();


/// SIGNUP
///
/// création d'un compte joueur si l'username n'existe pas déjà
/// crypte le mdp et ajoute le pays
app.post("/users/signup", async function(req, res, next) {
	const username = req.body.username;
	const password = req.body.password;
	const country = req.body.country;

	let sqlRequest = "SELECT * FROM users WHERE username = ?;";

	try {
		let [results] = await promisePool.execute(sqlRequest, [username.toString()]);

		if(results.length != 0) return res.sendStatus(403);

       	sqlRequest = "INSERT INTO users(id, username , password, noAds, softCurrency, hardCurrency, country) values (?, ?, ?, 0, 0, 0, ?);"
        
        let passwordHash = await bcrypt.hash(password, salt);
        const Id = ObjectId();

		await promisePool.execute(sqlRequest, [Id.toString(), username.toString(), passwordHash.toString(), country.toString()]);

		jwt.sign({id: Id}, secret, function (err, token) {
			if (err) return next(err);

			let infos = {
				username : null,
				password : null,
				token : token,
				id : Id
			};

		  	res.send(infos);
		});
	}

	catch (error) {
		return next(error);
	}
});


/// LOGIN
///
/// Connexion à un compte en fonction de l'username et du pseudo
/// renvoie une error si le compte ou le mdp est faux

app.post("/users/login", async function (req, res, next) {
	const username = req.body.username;
	const password = req.body.password;


	let sqlRequest = "SELECT * FROM users WHERE username = ?;";

	try {
		let [user] = await promisePool.execute(sqlRequest, [username]);

		if (!user.length) return res.sendStatus(404);

		let same = await bcrypt.compare(password, user[0].password);
		if (!same) return res.sendStatus(401);

		jwt.sign({id: user[0].id}, secret, function (err, token) {
			if (err) return next(err);
		  			  	
		  	let infos = {
				username : null,
				password : null,
				token : token,
				id : user[0].id
		  	}

		  	res.send(infos);
		});
	}

	catch (error) {
		return next(error);
	}
});									

/// PLAYERDATAS SAVE
///
/// Sauvegarde des données du joueur dans les tables respectives
///
/// update Users avec les variables NoAds, soft et hard currency, le pays via l'id unique du joueur
/// update / insert le level classé du jour avec son score, le nombre de coffres, le premier jour et le dernier jour joué via l'id unique du joueur
/// [....]
/// même chose pour les escaoudes du joueur, ses niveaux, etc
app.post("/playerdatas/save", async function (req, res, next) {

	let player_id = req.headers.authorization.substring(7);
	let playerJWT;

	jwt.verify(player_id, secret, function (err, player) {
		if (err) {
		  return res.send(err);
		} else playerJWT = player
	});


	player_id = playerJWT.id;


	let worlds = req.body.worlds;
	let rankedLevel = req.body.rankedLevel;
	let squads = req.body.squads;

	let noAds = req.body.noAds;
	let softCurrency = req.body.softCurrency;
	let hardCurrency = req.body.hardCurrency;
	let country = req.body.country;


	try {
		sqlRequest = "UPDATE users set noAds = ?, softCurrency = ?, hardCurrency = ?, country = ? WHERE id = ?;";
		await promisePool.execute(sqlRequest, [noAds, softCurrency, hardCurrency, country, player_id]);

		sqlRequest = "SELECT * FROM rankedLevel WHERE playerID = ?;";
		let [rankedLevelresult] = await promisePool.execute(sqlRequest, [player_id]);

		if(rankedLevel != undefined) {
			if(rankedLevelresult.length != 0) {
				sqlRequest = "UPDATE rankedLevel set score = ?, nbrOfChest = ?, StartDay = ?, LastPlayedDay = ? WHERE id = ?;";

				await promisePool.execute(sqlRequest, [rankedLevel.Score, rankedLevel.nbrOfChest, rankedLevel.StartDay, rankedLevel.LastPlayedDate, rankedLevel.id]);
			}
			else { 
				sqlRequest = "INSERT INTO rankedLevel (playerID, id, score, nbrOfChest, StartDay, LastPlayedDay) values (?, ?, ?, ?, ?, ?);"
				await promisePool.execute(sqlRequest, [player_id, rankedLevel.id, rankedLevel.Score, rankedLevel.nbrOfChest, rankedLevel.StartDay, rankedLevel.LastPlayedDate]);
			}
		}

		let squadsToInsert = [];
		let squadsToUpdate = [];

		sqlRequest = "SELECT * FROM squads WHERE playerID = ?;";
		let [squadsStored] = [];

		for (var i = squads.length - 1; i >= 0; i--) {
			sqlRequest = "SELECT * FROM squads WHERE id = ?;";
			[squadsStored] = await promisePool.execute(sqlRequest, [squads[i].id]);

			if(squadsStored.length <= 0) squadsToInsert.push(squads[i]);
			else if(squadsStored[0].level != squads[i].level)
						squadsToUpdate.push(squads[i]);
		}


		sqlRequest = "INSERT INTO squads (playerID, name, level, id) values";

		for (var l = squadsToInsert.length - 1; l >= 0; l--) {
			sqlRequest += " (" + `"${squadsToInsert[l].playerID}"` + ", " 
									 + `"${squadsToInsert[l].name}"` + ", " 
									 + `"${squadsToInsert[l].level}"` + ", "
									 + `"${squadsToInsert[l].id}"` + ")";
			if( l > 0) sqlRequest += ",";
		}
		sqlRequest += ";";

		if(squadsToInsert.length > 0) await promisePool.execute(sqlRequest);

		if(squadsToUpdate > 0)
			for (var m = squadsToUpdate.length - 1; m >= 0; m--) {
				sqlRequest = "UPDATE squads set level = ? where id = ?;";
				await promisePool.execute(sqlRequest, [squadsToUpdate[m].level, squadsToUpdate[m].id]);
			}


		let levelsToInsert = [];
		let levelsToUpdate = [];

		let [levels] = [];

		for (var i = worlds.length - 1; i >= 0; i--) {
			for (var j = worlds[i].levels.length - 1; j >= 0; j--) {
				sqlRequest = "SELECT * FROM levels WHERE id = ?;";
				[levels] = await promisePool.execute(sqlRequest, [worlds[i].levels[j].id]);

				if(levels.length <= 0) levelsToInsert.push(worlds[i].levels[j]);
				else if(levels[0].firstChest != worlds[i].levels[j].firstChest || levels[0].secondChest != worlds[i].levels[j].secondChest)
					levelsToUpdate.push(worlds[i].levels[j]);
			}
		}

		sqlRequest = "INSERT INTO levels (worldIndex, levelIndex, firstChest, secondChest, playerID, id) values";
		let firstbool = 0;
		let secondbool = 0;
		for (var l = levelsToInsert.length - 1; l >= 0; l--) {
			if(levelsToInsert[l].firstChest) firstbool = 1;
			else 0;

			if(levelsToInsert[l].secondChest) secondbool = 1;
			else 0;

			sqlRequest += " (" + `"${levelsToInsert[l].worldIndex}"` + ", " 
									 + `"${levelsToInsert[l].levelIndex}"` + ", " 
									 + `"${firstbool}"` + ", " 
									 + `"${secondbool}"` + ", " 
									 + `"${levelsToInsert[l].playerID}"` + ", " 
									 + `"${levelsToInsert[l].id}"` + ")";
			if( l > 0) sqlRequest += ",";
		}
		sqlRequest += ";";

		if(levelsToInsert.length > 0) await promisePool.execute(sqlRequest);

		if(levelsToUpdate > 0)
			for (var m = levelsToUpdate.length - 1; m >= 0; m--) {
				sqlRequest = "UPDATE levels set firstChest = ?, secondChest ? where id = ?;";
				await promisePool(sqlRequest, [levelsToUpdate[m].firstChest, levelsToUpdate[m].secondChest, levelsToUpdate[m].id]);
			}


		let worldsToInsert = [];
		let worldsToUpdate = [];

		sqlRequest = "SELECT * FROM worlds WHERE playerID = ?;";
		let [worldsStored] = [];

		for (var i = worlds.length - 1; i >= 0; i--) {
			sqlRequest = "SELECT * FROM worlds WHERE id = ?;";
			[worldsStored] = await promisePool.execute(sqlRequest, [worlds[i].id]);

			if(worldsStored.length <= 0) worldsToInsert.push(worlds[i]);
			else if(worldsStored[0].nbrOfChests != worlds[i].nbrOfChests)
						worldsToUpdate.push(worlds[i]);
		}

		sqlRequest = "INSERT INTO worlds (name, worldIndex, nbrOfChests, playerID, id) values";
		let isUnlock = 0;

		for (var l = worldsToInsert.length - 1; l >= 0; l--) {
			if(worldsToInsert[l].isUnlock) isUnlock = 1;
			else isUnlock = 0;

			sqlRequest += " (" + `"${worldsToInsert[l].name}"` + ", " 
									 + `"${worldsToInsert[l].worldIndex}"` + ", " 
									 + `"${worldsToInsert[l].nbrOfChests}"` + ", " 
									 + `"${worldsToInsert[l].playerID}"` + ", " 
									 + `"${worldsToInsert[l].id}"` + ")";
			if( l > 0) sqlRequest += ",";
		}
		sqlRequest += ";";

		if(worldsToInsert.length > 0) await promisePool.execute(sqlRequest);

		if(worldsToUpdate > 0)
			for (var m = worldsToUpdate.length - 1; m >= 0; m--) {
				sqlRequest = "UPDATE levels set nbrOfChests = ? where id = ?;";
				await promisePool(sqlRequest, [worldsToUpdate[m].nbrOfChests, worldsToUpdate[m].id]);
			}

		res.sendStatus(200);
	}
	catch (error) {
		return next(error);
	}
});


/// PLAYERDATAS / GET
///
/// Récuperation des données du joueur à l'aide de son id
/// renvoie un json contenant toutes les informations que l'on a trouvé
app.get("/playerdatas/get", async function (req, res, next) {

	let player_id = req.headers.authorization.substring(7);
	let playerJWT;

	jwt.verify(player_id, secret, function (err, player) {
		if (err) {
		  return res.send(err);
		} else playerJWT = player
	});


	player_id = playerJWT.id;

	let playerDatas = {
		worlds : [],
		rankedLevel : null,
		squads : [],
		noAds : false,
		softCurrency : 0,
		hardCurrency : 0,
		country : 0
	};


	let sqlRequest = "SELECT * FROM squads WHERE playerID = ?;";

	try {
		let sqlRequest = "SELECT * FROM squads WHERE playerID = ?;";
		let [squads] = await promisePool.execute(sqlRequest, [player_id]);
		playerDatas.squads = squads;

		sqlRequest = "SELECT * FROM worlds WHERE playerID = ?;";
		let [worlds] = await promisePool.execute(sqlRequest, [player_id]);

		sqlRequest = "SELECT * FROM levels WHERE playerID = ?;";
		let [levels] = await promisePool.execute(sqlRequest, [player_id]);


		for (var i = worlds.length - 1; i >= 0; i--) {
			let world = {
				name : worlds[i].name,
				worldIndex : worlds[i].worldIndex,
				nbrOfChests : worlds[i].nbrOfChests,
				levels : [],
				playerID : worlds[i].playerID,
				id : worlds[i].id
			};

			for (var j = levels.length - 1; j >= 0; j--) {
				if(levels[j].worldIndex == world.worldIndex){
					world.levels.push(levels[j]);
				}
			}

			playerDatas.worlds.push(world);
		}

		sqlRequest = "SELECT * FROM rankedLevel WHERE playerID = ?;";
		let [rankedLevel] = await promisePool.execute(sqlRequest, [player_id]);

		playerDatas.rankedLevel = rankedLevel[0];
		playerDatas.rankedLevel.LastPlayedDate = rankedLevel[0].LastPlayedDay;

		sqlRequest = "SELECT noAds, softCurrency, hardCurrency, country FROM users WHERE id = ?;";

		let [noAdsResult] = await promisePool.execute(sqlRequest, [player_id]);
		playerDatas.noAds = noAdsResult[0].noAds;
		playerDatas.softCurrency = noAdsResult[0].softCurrency;
		playerDatas.hardCurrency = noAdsResult[0].hardCurrency;
		playerDatas.country = noAdsResult[0].country;

		res.send(playerDatas);
	}

	catch (error) {
		return next(error);
	}	
});


/// WORLD SAVE
///
/// sauvegarder des infos sur le monde que l'on renvoie
app.post("/world/save", async function (req, res, next) {
	const name = req.body.name;
	const worldIndex = req.body.worldIndex;
	const nbrOfChests = req.body.nbrOfChests;
	const playerID = req.body.playerID;					
	const id = req.body.id;					

	let sqlRequest = "SELECT * FROM worlds WHERE id = ?;";

	try {
		let [results] = await promisePool.execute(sqlRequest, [id]);

		if(results.length != 0) {
			if(results[0].nbrOfChests != nbrOfChests){

				sqlRequest = "UPDATE worlds set nbrOfChests = ? where id = ?;";
				await promisePool.execute(sqlRequest, [nbrOfChests, id]);
			
				res.sendStatus(200);
			} else res.sendStatus(200);

		} else {

       	sqlRequest = "INSERT INTO worlds (name, worldIndex, nbrOfChests, playerID, id) values(?, ?, ?, ?, ?);",
        
		await promisePool.execute(sqlRequest, [name, worldIndex, nbrOfChests, playerID, id]);		
		res.sendStatus(200);
		}
	}

	catch (error) {
		return next(error);
	}
});

/// LEVEL SAVE
///
/// sauvegarder des infos sur le niveau que l'on renvoie
app.post("/level/save", async function (req, res, next) {
	const worldIndex = req.body.worldIndex;
	const levelIndex = req.body.levelIndex;
	const levelID = req.body.id;
	const firstChest = req.body.firstChest;
	const secondChest = req.body.secondChest;
	const playerID = req.body.playerID;					

	let sqlRequest = "SELECT * FROM levels WHERE id = ?;";

	try {
		let [results] = await promisePool.execute(sqlRequest, [levelID]);

		if(results.length != 0) {

			if(results[0].firstChest != firstChest || results[0].secondChest != secondChest){

				sqlRequest = "UPDATE levels set firstChest = ?, secondChest ? where id = ?;";
				await promisePool.execute(sqlRequest, [firstChest, secondChest, levelID]);
			
				res.sendStatus(200);
			} else res.sendStatus(200);

		}

       	sqlRequest = "INSERT INTO levels (worldIndex, levelIndex, firstChest, secondChest, playerID, id) values(?, ?, ?, ?, ?, ?);",
        
		await promisePool.execute(sqlRequest, [worldIndex, levelIndex, firstChest, secondChest, playerID, levelID]);		
		res.sendStatus(200);
	}

	catch (error) {
		return next(error);
	}
});

/// RANKEDLEVEL SAVE
///
/// sauvegarder des infos sur le niveau classé que l'on renvoie
app.post("/rankedLevel/save", async function (req, res, next) {
	const rankedLevelID = req.body.id;
	const playerID = req.body.playerID;	

	const score = req.body.Score;
	const nbrOfChest = req.body.nbrOfChest;

	const StartDay = req.body.StartDay;
	const LastPlayedDate = req.body.LastPlayedDate;				


	try {
		let sqlRequest = "SELECT * FROM rankedLevel WHERE id = ?;";
		let [rankedLevelresult] = await promisePool.execute(sqlRequest, [rankedLevelID]);

		if(rankedLevelresult.length != 0) {
			sqlRequest = "UPDATE rankedLevel set score = ?, nbrOfChest = ?, StartDay = ?, LastPlayedDay = ? WHERE id = ?;";
			await promisePool.execute(sqlRequest, [score, nbrOfChest, StartDay, LastPlayedDate, rankedLevelID]);
		}
		else { 
			sqlRequest = "INSERT INTO rankedLevel (playerID, id, score, nbrOfChest, StartDay, LastPlayedDay) values (?, ?, ?, ?, ?, ?);"
			await promisePool.execute(sqlRequest, [playerID, rankedLevelID, score, nbrOfChest, StartDay, LastPlayedDate]);
		}
        res.sendStatus(200);
	}

	catch (error) {
		return next(error);
	}
});


/// LASTPUBDATE SAVE
///
/// sauvegarde le dernier horaire de la derniere pub que l'on a eu
app.post("/lastPubDate/save", async function (req, res, next) {
	const Hour = req.body.Hour;
	const Min = req.body.Min;
	const Day = req.body.Day;
	const Month = req.body.Month;
	const Year = req.body.Year;
	const playerID = req.body.playerID;		


	try {
		let sqlRequest = "SELECT * FROM lastPubDate WHERE playerID = ?;";
		let [result] = await promisePool.execute(sqlRequest, [playerID]);

		if(result.length != 0) {
			sqlRequest = "UPDATE lastPubDate set Hour = ?, Minute = ?, Day = ?, Month = ?, Year = ? WHERE playerID = ?;";
			await promisePool.execute(sqlRequest, [Hour, Min, Day, Month, Year, playerID]);
		}
		else { 
			sqlRequest = "INSERT INTO lastPubDate (playerID, Hour, Minute, Day, Month, Year) values (?, ?, ?, ?, ?, ?);";
			await promisePool.execute(sqlRequest, [playerID, Hour, Min, Day, Month, Year]);
		}
        res.sendStatus(200);
	}

	catch (error) {
		return next(error);
	}
});


/// LASTPUBDATE GET
///
/// Recuperation du dernier horaire de la derniere pub que l'on a eu
app.post("/lastPubDate/get", async function (req, res, next) {
	const playerID = req.body.id;

	try {
		let sqlRequest = "SELECT * FROM lastPubDate WHERE playerID = ?;";
		let [result] = await promisePool.execute(sqlRequest, [playerID]);

		if(result.length != 0) {
			let date = {
				playerID : result[0].playerID,
				Hour : result[0].Hour,
				Min : result[0].Minute,
				Day : result[0].Day,
				Month : result[0].Month,
				Year : result[0].Year,
			};

			res.send(date);
		}
		else res.sendStatus(403);
	}
	catch (error) {
		return next(error);
	}
});


/// LEADERBOARD
///
/// récuperation du leaderboard du niveau classé
app.post("/leaderboard", async function (req, res, next) {

	const localization = req.body.localization;
	const startDay = req.body.startDay;	



	let sqlRequest ="";
	if(localization == "global") 
		sqlRequest = "SELECT * FROM rankedLevel INNER JOIN users ON rankedLevel.playerID = users.id WHERE StartDay = ? ORDER BY score DESC LIMIT 25;"
	else
		//sqlRequest = "SELECT * FROM rankedLevel WHERE StartDay = ? AND country = ? INNER JOIN users ON rankedLevel.playerID = users.id ORDER BY score DESC LIMIT 25;"
		sqlRequest = "SELECT * FROM rankedLevel INNER JOIN users ON rankedLevel.playerID = users.id WHERE StartDay = ? AND country = ? ORDER BY score DESC LIMIT 25;"

	try {

		let [results] = [];
		if(localization == "global")
			[results] = await promisePool.execute(sqlRequest, [startDay]);
		else [results] = await promisePool.execute(sqlRequest, [startDay, localization]);


		let leaderboard = [];

		for (var i = results.length - 1; i >= 0; i--) {
			leaderboard.push({
				name : results[i].username,
				rankedLevel : {
					playerID : results[i].playerID,
					Score : results[i].score,
					nbrOfChest : results[i].nbrOfChest,
					StartDay : startDay,
					LastPlayedDate : results[i].LastPlayedDay,
					id : results[i].id
				}
			})
		}	

		res.send(leaderboard);
	}

	catch (error) {
		return next(error);
	}
});


/// SQUAD SAVE
///
/// sauvegarde l'escouade envoyée
app.post("/squad/save", async function (req, res, next) {
	const name = req.body.name;
	const squadID = req.body.id;
	const playerID = req.body.playerID;
	const level = req.body.level;

	let sqlRequest = "SELECT * FROM squads WHERE id = ?;";

	try {
		let [results] = await promisePool.execute(sqlRequest, [squadID]);

		if(results.length != 0) {

			sqlRequest = "UPDATE squads set level = ? where id = ?;";

			await promisePool.execute(sqlRequest, [level, squadID]);	
			res.sendStatus(200);

		} else {

	       	sqlRequest = "INSERT INTO squads (playerID, name, level, id) values(?, ?, ?, ?);";
	        
			await promisePool.execute(sqlRequest, [playerID, name, level, squadID]);	
			res.sendStatus(200);
		}
	}

	catch (error) {
		return next(error);
	}
});

/// CURRENCIES SAVE
///
/// sauvegarde le montant des currencies envoyée en fonction de l'id du joueur
app.post("/currencies/save", async function (req, res, next) {

	const softCurrency = req.body.softCurrency;
	const hardCurrency = req.body.hardCurrency;

	let player_id = req.headers.authorization.substring(7);
	let playerJWT;

	jwt.verify(player_id, secret, function (err, player) {
		if (err) {
		  return res.send(err);
		} else playerJWT = player
	});


	player_id = playerJWT.id;

	let sqlRequest = "SELECT * FROM users WHERE id = ?;";

	try {
		let [results] = await promisePool.execute(sqlRequest, [player_id]);

		if(results.length != 0) {

			sqlRequest = "UPDATE users set softCurrency = ?, hardCurrency = ? WHERE id = ?;";

			await promisePool.execute(sqlRequest, [softCurrency, hardCurrency, player_id]);
		
			res.sendStatus(200);

		} else res.sendStatus(404);
	}

	catch (error) {
		return next(error);
	}
});


/// NOADS SAVE
///
/// sauvegarde du booleen du noAds 
app.post("/noAds/save", async function (req, res, next) {

	const noAds = req.body.noAds;

	let player_id = req.headers.authorization.substring(7);
	let playerJWT;

	jwt.verify(player_id, secret, function (err, player) {
		if (err) {
		  return res.send(err);
		} else playerJWT = player
	});


	player_id = playerJWT.id;

	let sqlRequest = "SELECT * FROM users WHERE id = ?;";

	try {
		let [results] = await promisePool.execute(sqlRequest, [player_id]);

		if(results.length != 0) {

			sqlRequest = "UPDATE users set noAds = ? WHERE id = ?;";

			await promisePool.execute(sqlRequest, [noAds, player_id]);
		
			res.sendStatus(200);

		} else res.sendStatus(404);
	}

	catch (error) {
		return next(error);
	}
});


app.use(function(req, res, next) {
	res.sendStatus(404);
});

app.use(function(err, req, res, next){
	res.status(500).send(err.stack);
});

const port = process.env.PORT || 8000;
app.listen(port, function (err) {
	if (err) console.error(err);
	else console.log("Listening to host :" + port);
});