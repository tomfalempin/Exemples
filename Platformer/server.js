const express = require("express");
const ObjectId = require("mongoose").Types.ObjectId;
const bcrypt = require("bcrypt");
const jwt = require("jsonwebtoken");
const app = express();
const cors = require("cors")
const mysql = require("mysql2");

const jsonMiddleware = express.json();
app.use(jsonMiddleware);
app.use(cors());


const secret = process.env.JWT_SECRET;

const jwtMiddleware = require("express-jwt")(
{
  secret: secret
}).unless(
{ 
  path: ["/users/signup", "/users/signin", /*"level/Save", "/level/leaderboard", */ "/test", "/level/localToOnline"] 
});

app.use(jwtMiddleware);

const db_infos = {
  host: process.env.DATABASE_HOST,
  user: process.env.DATABASE_USER,
  database: process.env.DATABASE_NAME,
  password: process.env.DATABASE_PASSWORD
};

let connection; 




/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
///                                                                                           ///
///                                      handleDisconnect                                     ///
///                                                                                           ///
///                                                                                           ///
///                  Appelé au debut de chaque requête si connection.done                     ///
///                                                                                           ///
///             Permet de se recreer la connection avec la base de donnée SQL                 ///
///                                                                                           ///
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////

function handleDisconnect() 
{
  connection = mysql.createConnection(db_infos);  

  connection.connect(function(err) {       
    if(err) {         
      //console.log('error when connecting to db:', err);
      setTimeout(handleDisconnect, 2000);       
    }                                            
  });                                            

  connection.on('error', function(err) {
    //console.log('db error', err);
    if(err.code === 'PROTOCOL_CONNECTION_LOST') { 
      handleDisconnect();                         
    } else {                                     
      throw err;                                  
    }
  });
}

handleDisconnect();





/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
///                                                                                           ///
///                                      GET /users/test                                      ///
///                                                                                           ///
///                                                                                           ///
///      Requête retournant un status 200, simplement pour vérifier si le côté client         ///
///                         réussi bien a communiqué avec le serveur                          ///
///                                                                                           ///
///                     Recreer aussi la connection si connection.done                        ///
///                                                                                           ///
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////

app.get("/test", function(req, res){
  if(connection.done) handleDisconnect();

  res.sendStatus(200);
});





/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
///                                                                                           ///
///                                     POST /users/signup                                    ///
///                                                                                           ///
///      Requête permettant au client de creer un nouveau compte si l'username n'est          ///
///                        pas encore utilisé dans la base de données                         ///
///                                                                                           ///
///                        retourne un status 404 si : BDD non trouvée                        ///
///                        retourne un status 409 si : Conflict                               ///
///                                                                                           ///
///                         retourne un token  si : Compté créé                               ///
///                                                                                           ///
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////

app.post("/users/signup", function (req, res, next) {

//BASE DE DONNEE CLEVER CLOUD
    if(connection.done) handleDisconnect();

    const username = req.body.username;

    let query = "SELECT * FROM Players WHERE name = " + `"${username}"`;

    connection.query(query, function(err, result){
      if(err) {
        //console.log(err)
        return res.sendStatus(404);
      }

      if(result.length <= 0){
        
        bcrypt.hash(req.body.password, 10, function(err, password) {
          if(err) {
            console.log(err);
            return next(err);
          }

          const id = ObjectId();

          query = 'INSERT INTO Players(name, id, password, maxlevel) values ('+`"${username}"`+', '+`"${id}"`+','+`"${password}"`+', 0)';

          connection.query(query, (err) => {
            if(err){
              //console.log(err);
              return res.sendStatus(409);
            }

            jwt.sign({ id: id }, secret, function (err, token) {
              if (err){
                //console.log(err);
                return res.sendStatus(err)
              }

              return res.send(token);
              //hello there !//
            });
          });
        });
      } else return res.sendStatus(409);
    });
});





/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
///                                                                                           ///
///                                     POST /users/signin                                    ///
///                                                                                           ///
///                  Requête permettant au client de se connecter à un compte                 ///
///                  déjà existant à l'un d'un mot de passe et d'un username                  ///
///                                                                                           ///
///                        retourne un status 404 si : BDD non trouvée                        ///
///                        retourne un status 404 si : Compte non trouvé                      ///
///                        retourne un status 403 si : Mot de passe incorrect                 ///
///                                                                                           ///
///                       retourne un token  si : Connexion au compte réussie                 ///
///                                                                                           ///
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////

app.post("/users/signin", function (req, res, next) {

  //BASE DE DONNEE CLEVER CLOUD
  if(connection.done) handleDisconnect();

  const username = req.body.username;
  const password = req.body.password;

  let query = "SELECT * FROM Players WHERE name = " + `"${username}"`;

  connection.query(query, function(err, result){
    if(err) {
      //console.log(err)
      return res.sendStatus(404);
    }
    
    if(result.length <= 0) return res.sendStatus(404);

    bcrypt.compare(password, result[0].password, function(err, same){
      if(err){
        //console.log(err);
        return next(err);
      }
      else if(!same) return res.sendStatus(401);

      else 
      {
        jwt.sign({id : result[0].id}, secret, function(err, token){
          if(err){
            //console.log(err);
            return next(err);
          } else {
            return res.send(token);
          }
       });
      }
    });
  });
});





/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
///                                                                                           ///
///                                 POST /level/localToOnline                                 ///
///                                                                                           ///
///                  Requête permettant au client de se synchroniser un compte                ///
///                   local vers une sauvegarde stockée sur la base de données                ///
///                                                                                           ///
///                  Créer un compte si le compte local n'existe pas sur la BDD               ///                                                                                          ///
///                                                                                           ///
///                        retourne un status 200 si : synchronisation réussite               ///
///                        retourne un status 404 si : BDD non trouvée                        ///
///                        retourne un status 403 si : Mot de passe incorrect                 ///
///                        retourne un status 409 si : Conflict                               ///
///                                                                                           ///
///                    UPDATE le score si besoin, sinon ne fait qu'un INSERT INTO             ///
///                                                                                           ///
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////

app.post("/level/localToOnline", function(req, res){
  if(connection.done) handleDisconnect();

  const username = req.body.name;
  const currentPassword = req.body.password;

  const level = req.body.level;

  const score = req.body.score;
  const objects = req.body.objects;
  const lives = req.body.lives;
  const time = req.body.time;

  let player_id;

  let currentToken;

  console.log("localToOnline");


  let query = "SELECT * FROM Players WHERE name = " + `"${username}"`;

  connection.query(query, function(err, result){
    if(err) {
      //console.log(err)
      return res.sendStatus(404);
    }

    if(result.length <= 0){
      bcrypt.hash(currentPassword, 10, function(err, password) {
        if(err) {
          //console.log(err);
          return next(err);
        }

        const id = ObjectId();

        query = 'INSERT INTO Players(name, id, password, maxlevel) values ('+`"${username}"`+', '+`"${id}"`+','+`"${password}"`+', 0)';
        connection.query(query, (err) => {
          if(err){
            //console.log(err);
            return res.sendStatus(409);
          } else {
            jwt.sign({ id: id }, secret, function (err, token) {
              if (err){
                //console.log(err);
                return res.sendStatus(err)
              }
              currentToken = token;

              jwt.verify(currentToken, secret, function (err, player) {
                if (err) {
                  //console.log(err);
                  return send(err);
                } else player_id = player.id;
              });

              query = "SELECT * from Level"+`${level}` +" where player_id =" + `"${player_id}"`;

              connection.query(query, (err, result) => {
                if (err) {
                  //console.log(err)
                  return res.sendStatus(404);
                } 
                else {
                  if(result.length <= 0)
                    query = 'INSERT INTO  Level'+`${level}` +'(player_id, score, objects, lives, time) values ('+`"${player_id}"`+', ' + `${score}`+ ', ' + `${objects}`+ ', '+ `${lives}`+ ', '+ `${time}`+')';
                  else 
                    query = 'UPDATE Level'+`${level}` + " set time = "+ `${time}`+ ", score = "+ `${score}` + ", objects = "+ `${objects}` + ", lives = "+ `${lives}`+ " where player_id =" + `"${player_id}"`;

                  connection.query(query, (err, row) => {
                    if (err) {
                      //console.log(err)
                      return res.sendStatus(404);
                    } 
                    else {
                      return res.send(200);
                    }
                  });            
                }
              });
            });
          }
        });
      });
    }
    else {

      bcrypt.compare(currentPassword, result[0].password, function(err, same){
        if(err){
          //console.log(err);
          return next(err);
        }
        else if(!same){
          return res.sendStatus(401);
        }
        else 
          {
          jwt.sign({id : result[0].id}, secret, function(err, token){
            if(err){
              //console.log(err);
              return next(err);
            } else {
              {
                currentToken = token;

                jwt.verify(currentToken, secret, function (err, player) {
                  if (err) {
                    //console.log(err);
                    return send(err);
                  } else player_id = player.id;
                });

                query = "SELECT * from Level"+`${level}` +" where player_id =" + `"${player_id}"`;

                connection.query(query, (err, result) => {
                  if (err) {
                    //console.log(err)
                    return res.sendStatus(404);
                  } 
                  else {
                    if(result.length <= 0)
                      query = 'INSERT INTO  Level'+`${level}` +'(player_id, score, objects, lives, time) values ('+`"${player_id}"`+', ' + `${score}`+ ', ' + `${objects}`+ ', '+ `${lives}`+ ', '+ `${time}`+')';
                    else 
                      query = 'UPDATE Level'+`${level}` + " set time = "+ `${time}`+ ", score = "+ `${score}` + ", objects = "+ `${objects}` + ", lives = "+ `${lives}`+ " where player_id =" + `"${player_id}"`;

                    connection.query(query, (err, row) => {
                      if (err) {
                        //console.log(err)
                        return res.sendStatus(404);
                      } 
                      else {
                        return res.send(200);
                      }
                    });            
                  }
                });
              }
            }
          });
        }
      });
    }
  });
});




/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
///                                                                                           ///
///                                      POST /level/save                                     ///
///                                                                                           ///
///                  Requête permettant au client de sauvegarder son score sur                ///
///                       la base de données en fonction du level en cours                    ///
///                                                                                           ///                                                                                        ///
///                        retourne un status 200 si : sauvegarde réussite                    ///
///                        retourne un status 404 si : BDD non trouvée                        ///
///                                                                                           ///
///                    UPDATE le score si besoin, sinon ne fait qu'un INSERT INTO             ///
///                                                                                           ///
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////

app.post("/level/save", function (req, res) {

  if(connection.done) handleDisconnect();

  console.log("save test : " + req.body.token);

  // BASE DE DONNEES CLEVER CLOUD
  let level = req.body.level;
  let time = req.body.time;
  let score = req.body.score;
  let objects = req.body.objects;
  let live = req.body.lives;

  let token = req.body.token;
  let player_id;

  jwt.verify(token, secret, function (err, player) {
    if (err) {
      //console.log(err);
      return send(err);
    } else player_id = player.id;
  });

let query = "SELECT * from Level"+`${level}` +" where player_id =" + `"${player_id}"`;
 
  connection.query(query, (err, result) => {
    if (err) {
      //console.log(err)
      return res.sendStatus(404);
    } 
    else {
      if(result.length <= 0)
        query = 'INSERT INTO  Level'+`${level}` +'(player_id, score, objects, lives, time) values ('+`"${player_id}"`+', ' + `${score}`+ ', ' + `${objects}`+ ', '+ `${live}`+ ', '+ `${time}`+')';
      else {
        if((result[0].score < score) || (result[0].score == score && result[0].objects < objects) || (result[0].score == score && result[0].objects == objects && result[0].time > time))
          query = 'UPDATE Level'+`${level}` + " set time = "+ `${time}`+ ", score = "+ `${score}` + ", objects = "+ `${objects}` + ", lives = "+ `${live}`+ " where player_id =" + `"${player_id}"`;
        else return res.sendStatus(200);
      }

      connection.query(query, (err, row) => {
        if (err) {
          //console.log(err)
          return res.sendStatus(404);
        } 
        else {
          return res.sendStatus(200);
        }
      });            
    }
  });  
});





/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////
///                                                                                           ///
///                                  POST /level/leaderboard                                  ///
///                                                                                           ///
///                  Requête permettant au client de récupérer le leaderboard d'un            ///
///                       level précis, renvoie le top 5 de la table récupérée                ///
///                                                                                           ///                                                                                        ///
///                        retourne un status 404 si : BDD non trouvée                        ///
///                                                                                           ///
///                    retourne une liste d'objects si : top 5 bien récupéré                  ///
///                                                                                           ///
/////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////

app.post("/level/leaderboard", function (req, res) {

  if(connection.done) handleDisconnect();

  let query = "SELECT score, objects, lives, time, name from Level"+`${req.body.level}` +" INNER JOIN Players ON Level"+`${req.body.level}` + ".player_id = Players.id ORDER BY score DESC LIMIT 5";

  connection.query(query, (err, results) => {
    if (err) {
      //console.log(err)
      return res.sendStatus(404);
    }
    //console.log(results);
    return res.send(results);
  });  
});





app.use(function(err, req, res, next){
  res.status(500).send(err)
});


const port = process.env.PORT || 8000;
app.listen(port, function (err) {
  if (err) console.error(err);
  else console.log("Listening to http://localhost:" +port);
});