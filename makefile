build:
	xbuild LastYearsWishes.sln /p:Configuration=Release

build-debug:
	xbuild LastYearsWishes.sln /p:Configuration=Debug

deploy:
	cp -r lib $(SRV)/
	cp -r static/* $(SRV)/static/
	cp -r bin $(SRV)/
	cp -r web.config $(SRV)/
	cp Global.asax Global.asax.cs $(SRV)/

